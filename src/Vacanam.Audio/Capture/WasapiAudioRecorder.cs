using NAudio.Wave;
using NAudio.CoreAudioApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Audio.Capture;

/// <summary>
/// Production IAudioRecorder implementation using NAudio WASAPI shared-mode capture.
/// Uses pure C# AudioConverter for instant, zero-dependency conversion to 16 kHz 16-bit mono PCM.
/// </summary>
public sealed class WasapiAudioRecorder : IAudioRecorder
{
    private readonly ILogger<WasapiAudioRecorder> _logger;
    private readonly AppSettings _settings;

    private WasapiCapture? _capture;
    private volatile float _audioLevel;
    private volatile bool _isRecording;
    private bool _disposed;

    public event EventHandler<AudioDataEventArgs>? DataAvailable;
    public double AudioLevel => _audioLevel;
    public bool IsRecording => _isRecording;

    public WasapiAudioRecorder(
        IOptions<AppSettings> settings,
        ILogger<WasapiAudioRecorder> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    // ── IAudioRecorder ────────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isRecording)
        {
            _logger.LogWarning("StartAsync called while already recording. Ignoring.");
            return;
        }

        try
        {
            _logger.LogInformation("Starting WASAPI audio capture.");

            var device = SelectCaptureDevice();
            _capture = new WasapiCapture(device, useEventSync: true, audioBufferMillisecondsLength: 50);

            _logger.LogInformation(
                "WASAPI device selected: {Name}, native format: {Rate} Hz {Bits}-bit {Ch}ch",
                device.FriendlyName,
                _capture.WaveFormat.SampleRate,
                _capture.WaveFormat.BitsPerSample,
                _capture.WaveFormat.Channels);

            _capture.DataAvailable += OnWasapiDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;

            _capture.StartRecording();
            _isRecording = true;

            _logger.LogInformation("WASAPI capture started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WASAPI audio capture.");
            CleanupCapture();
            throw new Core.Exceptions.AudioDeviceException("Failed to start microphone capture.", ex);
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRecording)
        {
            _logger.LogDebug("StopAsync called but not recording.");
            return;
        }

        _logger.LogInformation("Stopping WASAPI audio capture.");
        _isRecording = false;
        _audioLevel = 0f;

        _capture?.StopRecording();
        await Task.Delay(50, cancellationToken); // brief pause to allow buffer flush

        CleanupCapture();
        _logger.LogInformation("WASAPI capture stopped.");
    }

    public IReadOnlyList<AudioDevice> GetAvailableDevices()
    {
        var devices = new List<AudioDevice>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                bool isDefault = device.ID == defaultDevice.ID;
                devices.Add(new AudioDevice(
                    Id: device.ID,
                    Name: device.FriendlyName,
                    IsDefault: isDefault));
            }

            _logger.LogDebug("Enumerated {Count} capture devices.", devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate audio devices. Returning empty list.");
        }
        return devices;
    }

    // ── WASAPI Callbacks ──────────────────────────────────────────────────────

    private void OnWasapiDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded <= 0 || _capture is null) return;

        // Compute RMS level for UI meter
        UpdateAudioLevel(e.Buffer, e.BytesRecorded, _capture.WaveFormat);

        // Convert raw WASAPI buffer → 16 kHz 16-bit mono PCM bytes for Whisper
        byte[] pcm16Chunk = AudioConverter.To16kHzMonoPcm16(e.Buffer, e.BytesRecorded, _capture.WaveFormat);
        if (pcm16Chunk.Length > 0)
        {
            DataAvailable?.Invoke(this, new AudioDataEventArgs(pcm16Chunk, pcm16Chunk.Length));
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            _logger.LogError(e.Exception, "WASAPI recording stopped with error.");
        else
            _logger.LogDebug("WASAPI recording stopped cleanly.");
    }

    // ── Audio Level Metering ──────────────────────────────────────────────────

    private void UpdateAudioLevel(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        try
        {
            double rms = 0;
            if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            {
                int sampleCount = bytesRecorded / 4;
                double sumSq = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    float sample = BitConverter.ToSingle(buffer, i * 4);
                    sumSq += sample * sample;
                }
                rms = sampleCount > 0 ? Math.Sqrt(sumSq / sampleCount) : 0;
            }
            else if (format.BitsPerSample == 16)
            {
                int sampleCount = bytesRecorded / 2;
                double sumSq = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    float sample = BitConverter.ToInt16(buffer, i * 2) / 32768f;
                    sumSq += sample * sample;
                }
                rms = sampleCount > 0 ? Math.Sqrt(sumSq / sampleCount) : 0;
            }

            _audioLevel = (float)Math.Min(1.0, 0.3 * rms + 0.7 * _audioLevel);
        }
        catch
        {
            _audioLevel = 0f;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private MMDevice SelectCaptureDevice()
    {
        using var enumerator = new MMDeviceEnumerator();

        var preferredId = _settings.Audio.PreferredDeviceId;
        if (!string.IsNullOrEmpty(preferredId))
        {
            try
            {
                var preferred = enumerator.GetDevice(preferredId);
                if (preferred is not null && preferred.State == DeviceState.Active)
                {
                    _logger.LogInformation("Using preferred microphone: {Name}", preferred.FriendlyName);
                    return preferred;
                }
            }
            catch
            {
                _logger.LogWarning("Preferred device ID {Id} not found. Falling back to default.", preferredId);
            }
        }

        var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        _logger.LogInformation("Using default microphone: {Name}", defaultDevice.FriendlyName);
        return defaultDevice;
    }

    private void CleanupCapture()
    {
        if (_capture is not null)
        {
            _capture.DataAvailable -= OnWasapiDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
            _capture = null;
        }
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_isRecording)
            StopAsync().GetAwaiter().GetResult();
        CleanupCapture();
        _logger.LogDebug("WasapiAudioRecorder disposed.");
    }
}
