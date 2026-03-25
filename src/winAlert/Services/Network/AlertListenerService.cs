using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using winAlert.Domain.Events;
using winAlert.Domain.Models;
using winAlert.Services.Core;

namespace winAlert.Services.Network;

/// <summary>
/// Interface for the TCP alert listener service.
/// </summary>
public interface IAlertListenerService : IDisposable
{
    /// <summary>
    /// Starts listening on the configured port.
    /// </summary>
    Task StartAsync(int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the listener.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Whether the service is currently listening.
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Current port being listened on.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Number of active connections.
    /// </summary>
    int ConnectionCount { get; }
}

/// <summary>
/// Asynchronous TCP server that listens for incoming alert messages.
/// </summary>
public sealed class AlertListenerService : IAlertListenerService
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IAlertParser _parser;
    private readonly ILogger _logger;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new();
    private int _port;
    private bool _disposed;

    public AlertListenerService(IEventAggregator eventAggregator, IAlertParser parser, ILogger logger)
    {
        _eventAggregator = eventAggregator;
        _parser = parser;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    public int Port => _port;

    /// <inheritdoc />
    public int ConnectionCount => _clients.Count;

    /// <inheritdoc />
    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (IsListening)
        {
            _logger.Warning("Listener is already running on port {Port}", _port);
            return;
        }

        _port = port;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            IsListening = true;
            _logger.Information("Alert listener started on port {Port}", _port);

            _eventAggregator.Publish(new ListenerStatusChangedEvent(true, _port, 0));

            _acceptTask = AcceptClientsAsync(_cts.Token);
            await Task.CompletedTask;
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            IsListening = false;
            _logger.Error(ex, "Failed to start listener on port {Port}", _port);
            _eventAggregator.Publish(new ListenerStatusChangedEvent(false, _port, 0, ex.Message));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (!IsListening)
            return;

        _logger.Information("Stopping alert listener...");

        _cts?.Cancel();

        // Close all client connections
        foreach (var client in _clients.Values)
        {
            try
            {
                client.Close();
            }
            catch
            {
                // Ignore errors during shutdown
            }
        }
        _clients.Clear();

        // Stop the listener
        _listener?.Stop();
        _listener = null;

        if (_acceptTask != null)
        {
            try
            {
                await _acceptTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore cancellation
            }
        }

        IsListening = false;
        _logger.Information("Alert listener stopped");

        _eventAggregator.Publish(new ListenerStatusChangedEvent(false, _port, 0));
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                var clientId = Guid.NewGuid();

                if (_clients.TryAdd(clientId, client))
                {
                    _ = HandleClientAsync(clientId, client, cancellationToken);
                    _eventAggregator.Publish(new ListenerStatusChangedEvent(true, _port, _clients.Count));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error accepting client connection");
            }
        }
    }

    private async Task HandleClientAsync(Guid clientId, TcpClient client, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        _logger?.Debug("[LISTENER] HandleClientAsync started for client {ClientId}", clientId);
        Debug.WriteLine($"[LISTENER] HandleClientAsync started for client {clientId}");

        try
        {
            var stream = client.GetStream();
            stream.ReadTimeout = 30000; // 30 second timeout

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    // Client disconnected - process any remaining content
                    _logger?.Debug("[LISTENER] Client {ClientId} disconnected (0 bytes read)", clientId);
                    Debug.WriteLine($"[LISTENER] Client {clientId} disconnected (0 bytes read)");
                    
                    // Process any remaining content as a complete message
                    var remainingContent = messageBuilder.ToString().Trim();
                    if (!string.IsNullOrEmpty(remainingContent))
                    {
                        _logger?.Debug("[LISTENER] Processing remaining content: {Length} chars", remainingContent.Length);
                        Debug.WriteLine($"[LISTENER] Processing remaining content: {remainingContent.Length} chars");
                        ProcessMessage(remainingContent);
                    }
                    break;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                _logger?.Debug("[LISTENER] Received {Bytes} bytes from client {ClientId}", bytesRead, clientId);
                Debug.WriteLine($"[LISTENER] Received {bytesRead} bytes from client {clientId}");
                messageBuilder.Append(chunk);

                // Try to extract complete JSON objects by counting braces
                var content = messageBuilder.ToString();
                var completeMessages = TryExtractCompleteJsonMessages(content);
                
                if (completeMessages.Count > 0)
                {
                    _logger?.Debug("[LISTENER] Extracted {Count} complete JSON messages", completeMessages.Count);
                    Debug.WriteLine($"[LISTENER] Extracted {completeMessages.Count} complete JSON messages");
                    
                    foreach (var message in completeMessages)
                    {
                        ProcessMessage(message);
                    }
                    
                    // Clear the buffer after processing
                    messageBuilder.Clear();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            _logger?.Debug("Client {ClientId} disconnected: {Message}", clientId, ex.Message);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error handling client {ClientId}", clientId);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            client.Dispose();
            _eventAggregator.Publish(new ListenerStatusChangedEvent(true, _port, _clients.Count));
        }
    }

    /// <summary>
    /// Attempts to extract complete JSON objects from the content.
    /// Returns a list of complete JSON strings found.
    /// </summary>
    private List<string> TryExtractCompleteJsonMessages(string content)
    {
        var messages = new List<string>();
        var braceCount = 0;
        var startIndex = -1;
        
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '{')
            {
                if (braceCount == 0)
                {
                    startIndex = i;
                }
                braceCount++;
            }
            else if (content[i] == '}')
            {
                braceCount--;
                
                if (braceCount == 0 && startIndex >= 0)
                {
                    // Found a complete JSON object
                    var jsonMessage = content.Substring(startIndex, i - startIndex + 1).Trim();
                    messages.Add(jsonMessage);
                    startIndex = -1;
                }
                else if (braceCount < 0)
                {
                    // Malformed JSON - reset
                    braceCount = 0;
                    startIndex = -1;
                }
            }
        }
        
        return messages;
    }

    private void ProcessMessage(string json)
    {
        _logger?.Debug("[LISTENER] ProcessMessage called with JSON: {Length} chars", json.Length);
        Debug.WriteLine($"[LISTENER] ProcessMessage called with JSON: {json.Length} chars");

        var alert = _parser.Parse(json);

        if (alert == null)
        {
            _logger?.Warning("[LISTENER] Failed to parse alert message: {Message}", json.Length > 200 ? json[..200] + "..." : json);
            Debug.WriteLine($"[LISTENER] Failed to parse alert message: {json}");
            return;
        }

        _logger?.Information("[LISTENER] Received alert: {AlertId} [{Severity}] {Title}",
            alert.Id, alert.Severity, alert.Title);
        Debug.WriteLine($"[LISTENER] Received alert: {alert.Id} [{alert.Severity}] {alert.Title}");

        var notificationPlan = NotificationPlan.ForSeverity(alert.Severity);
        _logger?.Debug("[LISTENER] Publishing AlertReceivedEvent. PlayAudio: {PlayAudio}, ShowOverlay: {ShowOverlay}",
            notificationPlan.PlayAudio, notificationPlan.ShowOverlay);
        Debug.WriteLine($"[LISTENER] Publishing AlertReceivedEvent. PlayAudio: {notificationPlan.PlayAudio}, ShowOverlay: {notificationPlan.ShowOverlay}");

        _eventAggregator.Publish(new AlertReceivedEvent(alert, notificationPlan));

        _logger?.Debug("[LISTENER] AlertReceivedEvent published successfully");
        Debug.WriteLine("[LISTENER] AlertReceivedEvent published successfully");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();

        GC.SuppressFinalize(this);
    }
}
