using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;
using NAudio.Wave;
using Serilog;
using winAlert.Domain.Models;

namespace winAlert.Services.Notification;

/// <summary>
/// Interface for audio notification playback.
/// </summary>
public interface IAudioNotificationService : IDisposable
{
    /// <summary>
    /// Plays the audio notification for the given severity.
    /// </summary>
    void Play(AlertSeverity severity, float volume);

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    void Stop();

    /// <summary>
    /// Stops audio for a specific alert.
    /// </summary>
    void StopForAlert(Guid alertId);

    /// <summary>
    /// Sets the master mute state.
    /// </summary>
    void SetMute(bool muted);

    /// <summary>
    /// Whether audio is currently muted.
    /// </summary>
    bool IsMuted { get; }
}

/// <summary>
/// Audio notification service using NAudio for WAV playback.
/// </summary>
public sealed class AudioNotificationService : IAudioNotificationService
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, PlaybackInstance> _activePlaybacks = new();
    private readonly string _soundsPath;
    private bool _isMuted;
    private bool _disposed;

    public AudioNotificationService(ILogger logger)
    {
        _logger = logger;
        _soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sounds");
    }

    /// <inheritdoc />
    public bool IsMuted => _isMuted;

    /// <inheritdoc />
    public void Play(AlertSeverity severity, float volume)
    {
        _logger?.Debug("[AUDIO] Play called for {Severity} at volume {Volume}", severity, volume);
        Debug.WriteLine($"[AUDIO] Play called for {severity} at volume {volume}");

        if (_isMuted)
        {
            _logger?.Debug("[AUDIO] Skipping playback - audio is muted");
            Debug.WriteLine("[AUDIO] Skipping playback - audio is muted");
            return;
        }

        if (_disposed)
        {
            _logger?.Debug("[AUDIO] Skipping playback - service is disposed");
            Debug.WriteLine("[AUDIO] Skipping playback - service is disposed");
            return;
        }

        var alertId = Guid.NewGuid();
        var audioFile = severity.ToAudioFile();
        var filePath = Path.Combine(_soundsPath, audioFile);

        _logger?.Debug("[AUDIO] Looking for audio file: {FilePath}", filePath);
        Debug.WriteLine($"[AUDIO] Looking for audio file: {filePath}");

        try
        {
            if (!File.Exists(filePath))
            {
                _logger?.Warning("[AUDIO] Audio file not found: {FilePath}. Falling back to beep.", filePath);
                Debug.WriteLine($"[AUDIO] Audio file not found: {filePath}. Falling back to beep.");
                // Fall back to system beep
                SystemSounds.Beep.Play();
                return;
            }

            var loop = severity == AlertSeverity.Critical;
            _logger?.Debug("[AUDIO] Creating playback instance for {Severity}. Loop: {Loop}", severity, loop);
            Debug.WriteLine($"[AUDIO] Creating playback instance for {severity}. Loop: {loop}");
            
            var instance = new PlaybackInstance(alertId, filePath, volume, loop);

            if (_activePlaybacks.TryAdd(alertId, instance))
            {
                instance.PlaybackStateChanged += (_, _) => CheckPlaybackComplete(alertId);
                instance.Start();
                _logger?.Information("[AUDIO] Started playing audio for {Severity}: {FilePath}", severity, audioFile);
                Debug.WriteLine($"[AUDIO] Started playing audio for {severity}: {audioFile}");
            }
            else
            {
                _logger?.Warning("[AUDIO] Failed to add playback instance to active playbacks");
                Debug.WriteLine("[AUDIO] Failed to add playback instance to active playbacks");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to play audio for {Severity}", severity);
            // Fall back to system beep
            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
                // Ignore
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        foreach (var alertId in _activePlaybacks.Keys)
        {
            StopForAlert(alertId);
        }
    }

    /// <inheritdoc />
    public void StopForAlert(Guid alertId)
    {
        if (_activePlaybacks.TryRemove(alertId, out var instance))
        {
            instance.Stop();
            instance.Dispose();
            _logger.Debug("Stopped audio for alert {AlertId}", alertId);
        }
    }

    /// <inheritdoc />
    public void SetMute(bool muted)
    {
        _isMuted = muted;

        if (muted)
        {
            Stop();
        }
    }

    private void CheckPlaybackComplete(Guid alertId)
    {
        if (_activePlaybacks.TryGetValue(alertId, out var instance))
        {
            if (instance.PlaybackState != PlaybackState.Playing)
            {
                _activePlaybacks.TryRemove(alertId, out _);
                instance.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();

        GC.SuppressFinalize(this);
    }

    private sealed class PlaybackInstance : IDisposable
    {
        private readonly WaveOutEvent _waveOut;
        private readonly AudioFileReader _audioFile;
        private readonly bool _loop;
        private readonly Timer? _timer;
        private bool _disposed;

        public event EventHandler? PlaybackStateChanged;

        public Guid AlertId { get; }
        public PlaybackState PlaybackState => _waveOut.PlaybackState;

        public PlaybackInstance(Guid alertId, string filePath, float volume, bool loop)
        {
            AlertId = alertId;
            _loop = loop;

            _audioFile = new AudioFileReader(filePath) { Volume = volume };
            _waveOut = new WaveOutEvent { DesiredLatency = 200 };
            _waveOut.Init(_audioFile);
            _waveOut.PlaybackStopped += OnPlaybackStopped;

            if (loop)
            {
                // Check for loop completion every 100ms
                _timer = new Timer(_ => CheckLoop(), null, 100, 100);
            }
        }

        public void Start()
        {
            _waveOut.Play();
        }

        public void Stop()
        {
            _waveOut.Stop();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CheckLoop()
        {
            if (_disposed)
                return;

            try
            {
                if (_waveOut.PlaybackState != PlaybackState.Playing && _loop)
                {
                    // Restart playback for loop
                    _audioFile.Position = 0;
                    _waveOut.Play();
                }
            }
            catch
            {
                // Ignore errors during loop check
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _timer?.Dispose();
            _waveOut.Dispose();
            _audioFile.Dispose();
        }
    }
}
