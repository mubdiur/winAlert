using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;

namespace winAlert.Services.Core;

/// <summary>
/// Interface for event aggregation service.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Subscribe to an event type.
    /// </summary>
    void Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Unsubscribe from an event type.
    /// </summary>
    void Unsubscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    void Publish<TEvent>(TEvent eventToPublish);
}

/// <summary>
/// Thread-safe event aggregator for pub/sub messaging.
/// </summary>
public sealed class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();
    private readonly ILogger _logger;

    public EventAggregator(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[eventType] = handlers;
            }

            // Add new handler
            handlers.Add(handler);
            _logger?.Debug("[EVENTAGG] Subscribed to {EventType}. Total handlers: {Count}", eventType.Name, handlers.Count);
            Debug.WriteLine($"[EVENTAGG] Subscribed to {eventType.Name}. Total handlers: {handlers.Count}");
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.RemoveAll(d => ReferenceEquals(d, handler));
            }
        }
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent eventToPublish)
    {
        var eventType = typeof(TEvent);
        _logger?.Debug("[EVENTAGG] Publishing {EventType}", eventType.Name);
        Debug.WriteLine($"[EVENTAGG] Publishing {eventType.Name}");

        // Take a snapshot of handlers to avoid holding lock during invocation
        List<Delegate>? handlersSnapshot = null;

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                // Copy the list
                handlersSnapshot = new List<Delegate>(handlers);
            }
        }

        // Invoke handlers outside the lock
        if (handlersSnapshot != null)
        {
            _logger?.Debug("[EVENTAGG] Invoking {Count} handlers for {EventType}", handlersSnapshot.Count, eventType.Name);
            Debug.WriteLine($"[EVENTAGG] Invoking {handlersSnapshot.Count} handlers for {eventType.Name}");
            foreach (var del in handlersSnapshot)
            {
                if (del is Action<TEvent> handler)
                {
                    try
                    {
                        handler(eventToPublish);
                        _logger?.Debug("[EVENTAGG] Handler invoked successfully for {EventType}", eventType.Name);
                        Debug.WriteLine($"[EVENTAGG] Handler invoked successfully for {eventType.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex, "[EVENTAGG] Error invoking handler for {EventType}", eventType.Name);
                        Debug.WriteLine($"[EVENTAGG] Error invoking handler for {eventType.Name}: {ex.Message}");
                    }
                }
            }
        }
        else
        {
            _logger?.Warning("[EVENTAGG] No handlers registered for {EventType}", eventType.Name);
            Debug.WriteLine($"[EVENTAGG] WARNING: No handlers registered for {eventType.Name}");
        }
    }
}
