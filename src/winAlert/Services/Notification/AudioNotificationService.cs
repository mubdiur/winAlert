using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using winAlert.Domain.Models;

namespace winAlert.Services.Notification;

/// <summary>
/// Common interface for audio playback instances.
/// </summary>
internal interface IAudioPlaybackInstance : IDisposable
{
    Guid AlertId { get; }
    PlaybackState PlaybackState { get; }
    event EventHandler? PlaybackStateChanged;
    void Start();
    void Stop();
}

/// <summary>
/// Interface for audio notification playback.
/// </summary>
public interface IAudioNotificationService : IDisposable
{
    /// <summary>
    /// Plays the audio notification for the given severity.
    /// </summary>
    void Play(Guid alertId, AlertSeverity severity, float volume);

    /// <summary>
    /// Plays the siren sound for an unacknowledged alert.
    /// </summary>
    void PlaySiren(Guid alertId, float volume);

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    void Stop();

    /// <summary>
    /// Stops audio for a specific alert (including siren if playing).
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
    private readonly ConcurrentDictionary<Guid, IAudioPlaybackInstance> _activePlaybacks = new();
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
    public void Play(Guid alertId, AlertSeverity severity, float volume)
    {
        _logger?.Debug("[AUDIO] Play called for {AlertId} {Severity} at volume {Volume}", alertId, severity, volume);
        Debug.WriteLine($"[AUDIO] Play called for {alertId} {severity} at volume {volume}");

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
                _logger?.Information("[AUDIO] Started playing audio for {AlertId} {Severity}: {FilePath}", alertId, severity, audioFile);
                Debug.WriteLine($"[AUDIO] Started playing audio for {alertId} {severity}: {audioFile}");
            }
            else
            {
                _logger?.Warning("[AUDIO] Failed to add playback instance to active playbacks");
                Debug.WriteLine("[AUDIO] Failed to add playback instance to active playbacks");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to play audio for {AlertId} {Severity}", alertId, severity);
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
    public void PlaySiren(Guid alertId, float volume)
    {
        _logger?.Debug("[AUDIO] PlaySiren called for {AlertId} at volume {Volume}", alertId, volume);
        Debug.WriteLine($"[AUDIO] PlaySiren called for {alertId} at volume {volume}");

        if (_isMuted)
        {
            _logger?.Debug("[AUDIO] Skipping siren - audio is muted");
            Debug.WriteLine("[AUDIO] Skipping siren - audio is muted");
            return;
        }

        if (_disposed)
        {
            _logger?.Debug("[AUDIO] Skipping siren - service is disposed");
            Debug.WriteLine("[AUDIO] Skipping siren - service is disposed");
            return;
        }

        try
        {
            var instance = new SirenInstance(alertId, volume);

            if (_activePlaybacks.TryAdd(alertId, instance))
            {
                instance.PlaybackStateChanged += (_, _) => CheckPlaybackComplete(alertId);
                instance.Start();
                _logger?.Information("[AUDIO] Started playing siren for alert {AlertId}", alertId);
                Debug.WriteLine($"[AUDIO] Started playing siren for alert {alertId}");
            }
            else
            {
                _logger?.Warning("[AUDIO] Failed to add siren instance to active playbacks");
                Debug.WriteLine($"[AUDIO] Failed to add siren instance to active playbacks");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to play siren for {AlertId}", alertId);
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

    private sealed class PlaybackInstance : IAudioPlaybackInstance
    {
        private readonly WaveOutEvent _waveOut;
        private readonly AudioFileReader _audioFile;
        private readonly bool _loop;
        private readonly System.Threading.Timer? _timer;
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

    /// <summary>
    /// Square wave siren generator: 880Hz, 100ms on/100ms off, 60% volume.
    /// </summary>
    private sealed class SirenInstance : IAudioPlaybackInstance
    {
        private readonly WaveOutEvent _waveOut;
        private readonly SquareWaveProvider _sirenSource;
        private readonly System.Threading.Timer? _toggleTimer;
        private bool _isBeepOn = true;
        private bool _disposed;

        public event EventHandler? PlaybackStateChanged;

        public Guid AlertId { get; }
        public PlaybackState PlaybackState => _waveOut.PlaybackState;

        public SirenInstance(Guid alertId, float volume)
        {
            AlertId = alertId;

            // 880 Hz square wave at 60% volume (0.6)
            _sirenSource = new SquareWaveProvider(880, 0.6f);
            _waveOut = new WaveOutEvent { DesiredLatency = 200 };
            _waveOut.Init(_sirenSource);
            _waveOut.PlaybackStopped += OnPlaybackStopped;

            // Toggle beep on/off every 100ms
            _toggleTimer = new Timer(_ => ToggleBeep(), null, 100, 100);
        }

        private void ToggleBeep()
        {
            if (_disposed)
                return;

            try
            {
                _isBeepOn = !_isBeepOn;
                _sirenSource.Enabled = _isBeepOn;
            }
            catch
            {
                // Ignore
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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _toggleTimer?.Dispose();
            _waveOut.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Simple square wave provider at a fixed frequency.
    /// </summary>
    private sealed class SquareWaveProvider : WaveStream
    {
        private readonly float _frequency;
        private readonly float _volume;
        private long _position;
        private bool _enabled = true;

        public SquareWaveProvider(float frequency, float volume)
        {
            _frequency = frequency;
            _volume = volume;
        }

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public override WaveFormat WaveFormat { get; } = new WaveFormat(44100, 16, 1);

        public override long Length => long.MaxValue;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var samplesTotal = count / 2;
            var periodSamples = (int)(WaveFormat.SampleRate / _frequency);

            for (var i = 0; i < samplesTotal; i++)
            {
                var cyclePosition = (_position + i) % periodSamples;
                var sampleValue = cyclePosition < periodSamples / 2
                    ? (short)(short.MaxValue * _volume)
                    : (short)(-short.MaxValue * _volume);

                if (!_enabled)
                    sampleValue = 0;

                buffer[offset + i * 2] = (byte)(sampleValue & 0xFF);
                buffer[offset + i * 2 + 1] = (byte)((sampleValue >> 8) & 0xFF);
            }

            _position += samplesTotal;
            return count;
        }
    }
}
