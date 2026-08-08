namespace Vacanam.Core.Interfaces;

/// <summary>
/// Abstracts microphone capture. Implementations must be non-blocking and
/// raise DataAvailable on a background thread.
/// </summary>
public interface IAudioRecorder : IDisposable
{
    /// <summary>Raised when a new buffer of PCM audio data is available.</summary>
    event EventHandler<AudioDataEventArgs> DataAvailable;

    /// <summary>Current audio level (0.0 = silence, 1.0 = peak). Updated continuously while recording.</summary>
    double AudioLevel { get; }

    /// <summary>True if the recorder is currently capturing.</summary>
    bool IsRecording { get; }

    /// <summary>Starts capturing audio from the selected or default microphone.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops capturing and flushes any remaining audio.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns available microphone device names keyed by device ID.</summary>
    IReadOnlyList<AudioDevice> GetAvailableDevices();
}

/// <summary>Describes a captured audio buffer (16 kHz, 16-bit mono PCM).</summary>
public sealed class AudioDataEventArgs(byte[] buffer, int bytesRecorded) : EventArgs
{
    public byte[] Buffer { get; } = buffer;
    public int BytesRecorded { get; } = bytesRecorded;
}

/// <summary>Represents an available audio input device.</summary>
public sealed record AudioDevice(string Id, string Name, bool IsDefault);
