using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using winAlert.Domain.Models;

namespace winAlert.Services.Network;

/// <summary>
/// Parses and validates incoming JSON alert messages.
/// </summary>
public interface IAlertParser
{
    /// <summary>
    /// Parses a JSON string into an Alert object.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed Alert or null if invalid.</returns>
    Alert? Parse(string json);

    /// <summary>
    /// Validates a JSON string without parsing it fully.
    /// </summary>
    bool Validate(string json);
}

/// <summary>
/// JSON message parser for alert protocol.
/// </summary>
public sealed class AlertParser : IAlertParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly ILogger _logger;

    public AlertParser(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Alert? Parse(string json)
    {
        _logger?.Debug("[PARSER] Attempting to parse JSON: {Length} chars", json?.Length ?? 0);
        Debug.WriteLine($"[PARSER] Attempting to parse JSON: {json?.Length ?? 0} chars");

        if (string.IsNullOrWhiteSpace(json))
        {
            _logger?.Warning("[PARSER] JSON is null or whitespace");
            Debug.WriteLine("[PARSER] JSON is null or whitespace");
            return null;
        }

        try
        {
            _logger?.Debug("[PARSER] Deserializing JSON...");
            Debug.WriteLine("[PARSER] Deserializing JSON...");
            
            var dto = JsonSerializer.Deserialize<AlertDto>(json, Options);
            if (dto == null)
            {
                _logger?.Warning("[PARSER] Deserialization returned null");
                Debug.WriteLine("[PARSER] Deserialization returned null");
                return null;
            }

            _logger?.Debug("[PARSER] DTO deserialized successfully. Title: {Title}", dto.Title);
            Debug.WriteLine($"[PARSER] DTO deserialized. Title: {dto.Title}, Severity: {dto.Severity}");

            var alert = ConvertToAlert(dto);
            _logger?.Information("[PARSER] Alert parsed successfully: {AlertId} - {Title}", alert.Id, alert.Title);
            Debug.WriteLine($"[PARSER] Alert parsed: {alert.Id} - {alert.Title}");
            return alert;
        }
        catch (JsonException ex)
        {
            _logger?.Error(ex, "[PARSER] JSON parse error. JSON: {Json}", json.Length > 500 ? json[..500] + "..." : json);
            Debug.WriteLine($"[PARSER] JSON parse error: {ex.Message}");
            Debug.WriteLine($"[PARSER] JSON content: {(json.Length > 200 ? json[..200] + "..." : json)}");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "[PARSER] Unexpected error parsing JSON");
            Debug.WriteLine($"[PARSER] Unexpected error: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public bool Validate(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check required fields
            return root.TryGetProperty("title", out _) ||
                   root.TryGetProperty("message", out _);
        }
        catch
        {
            return false;
        }
    }

    private static Alert ConvertToAlert(AlertDto dto)
    {
        // Parse severity
        var severity = AlertSeverity.Info;
        if (!string.IsNullOrEmpty(dto.Severity))
        {
            if (Enum.TryParse<AlertSeverity>(dto.Severity, ignoreCase: true, out var parsed))
            {
                severity = parsed;
            }
        }

        // Parse timestamp
        var timestamp = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.Timestamp))
        {
            if (DateTime.TryParse(dto.Timestamp, out var parsed))
            {
                timestamp = parsed;
            }
        }

        // Parse metadata
        var metadata = new Dictionary<string, string>();
        if (dto.Metadata != null)
        {
            foreach (var kvp in dto.Metadata)
            {
                if (kvp.Value != null)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }
        }

        // Parse ID
        var id = Guid.NewGuid();
        if (!string.IsNullOrEmpty(dto.Id))
        {
            if (Guid.TryParse(dto.Id, out var parsed))
            {
                id = parsed;
            }
        }

        return new Alert
        {
            Id = id,
            Severity = severity,
            Source = dto.Source ?? string.Empty,
            Title = dto.Title ?? "Untitled Alert",
            Message = dto.Message ?? string.Empty,
            Timestamp = timestamp,
            Metadata = metadata,
            RequireAcknowledgment = dto.RequireAcknowledgment ?? true,
            AutoCloseSeconds = dto.AutoCloseSeconds ?? severity.GetDefaultAutoCloseSeconds()
        };
    }

    /// <summary>
    /// Internal DTO for JSON deserialization.
    /// </summary>
    private sealed class AlertDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string?>? Metadata { get; set; }

        [JsonPropertyName("requireAcknowledgment")]
        public bool? RequireAcknowledgment { get; set; }

        [JsonPropertyName("autoCloseSeconds")]
        public int? AutoCloseSeconds { get; set; }
    }
}