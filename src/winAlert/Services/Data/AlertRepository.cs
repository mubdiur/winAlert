using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using winAlert.Domain.Events;
using winAlert.Domain.Models;

namespace winAlert.Services.Data;

/// <summary>
/// Interface for alert storage and retrieval.
/// </summary>
public interface IAlertRepository
{
    /// <summary>
    /// Adds a new alert to the repository.
    /// </summary>
    void Add(Alert alert);

    /// <summary>
    /// Gets an alert by ID.
    /// </summary>
    Alert? Get(Guid id);

    /// <summary>
    /// Gets all active (unacknowledged) alerts.
    /// </summary>
    IReadOnlyList<Alert> GetActive();

    /// <summary>
    /// Gets all alerts in history.
    /// </summary>
    IReadOnlyList<Alert> GetHistory();

    /// <summary>
    /// Gets alerts filtered by severity.
    /// </summary>
    IReadOnlyList<Alert> GetBySeverity(AlertSeverity severity);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    bool Acknowledge(Guid id);

    /// <summary>
    /// Removes an alert from the repository.
    /// </summary>
    bool Remove(Guid id);

    /// <summary>
    /// Clears all acknowledged alerts.
    /// </summary>
    void ClearAcknowledged();

    /// <summary>
    /// Clears the entire alert history.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Gets the count of active alerts.
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    /// Gets the count of active alerts by severity.
    /// </summary>
    int GetActiveCountBySeverity(AlertSeverity severity);

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    double AverageResponseTimeMs { get; }

    /// <summary>
    /// Event raised when the repository changes.
    /// </summary>
    event EventHandler? RepositoryChanged;
}

/// <summary>
/// In-memory alert repository with event publishing.
/// </summary>
public sealed class AlertRepository : IAlertRepository
{
    private readonly ConcurrentDictionary<Guid, Alert> _alerts = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public event EventHandler? RepositoryChanged;

    /// <inheritdoc />
    public int ActiveCount => _alerts.Values.Count(a => !a.IsAcknowledged);

    /// <inheritdoc />
    public double AverageResponseTimeMs
    {
        get
        {
            var acknowledged = _alerts.Values.Where(a => a.ResponseTimeMs.HasValue).ToList();
            return acknowledged.Count > 0
                ? acknowledged.Average(a => a.ResponseTimeMs!.Value)
                : 0;
        }
    }

    /// <inheritdoc />
    public void Add(Alert alert)
    {
        // Check for duplicate ID
        if (_alerts.ContainsKey(alert.Id))
        {
            return; // Ignore duplicate
        }

        _alerts[alert.Id] = alert;
        OnRepositoryChanged();
    }

    /// <inheritdoc />
    public Alert? Get(Guid id)
    {
        _alerts.TryGetValue(id, out var alert);
        return alert;
    }

    /// <inheritdoc />
    public IReadOnlyList<Alert> GetActive()
    {
        return _alerts.Values
            .Where(a => !a.IsAcknowledged)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.ReceivedAt)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<Alert> GetHistory()
    {
        return _alerts.Values
            .OrderByDescending(a => a.ReceivedAt)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<Alert> GetBySeverity(AlertSeverity severity)
    {
        return _alerts.Values
            .Where(a => a.Severity == severity)
            .OrderByDescending(a => a.ReceivedAt)
            .ToList();
    }

    /// <inheritdoc />
    public bool Acknowledge(Guid id)
    {
        if (!_alerts.TryGetValue(id, out var alert))
            return false;

        try
        {
            alert.Acknowledge();
            OnRepositoryChanged();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false; // Already acknowledged
        }
    }

    /// <inheritdoc />
    public bool Remove(Guid id)
    {
        var removed = _alerts.TryRemove(id, out _);
        if (removed)
        {
            OnRepositoryChanged();
        }
        return removed;
    }

    /// <inheritdoc />
    public void ClearAcknowledged()
    {
        var acknowledgedIds = _alerts.Values
            .Where(a => a.IsAcknowledged)
            .Select(a => a.Id)
            .ToList();

        foreach (var id in acknowledgedIds)
        {
            _alerts.TryRemove(id, out _);
        }

        if (acknowledgedIds.Count > 0)
        {
            OnRepositoryChanged();
        }
    }

    /// <inheritdoc />
    public void ClearAll()
    {
        _alerts.Clear();
        OnRepositoryChanged();
    }

    /// <inheritdoc />
    public int GetActiveCountBySeverity(AlertSeverity severity)
    {
        return _alerts.Values.Count(a => !a.IsAcknowledged && a.Severity == severity);
    }

    private void OnRepositoryChanged()
    {
        RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
