using Vacanam.Core.Interfaces;

namespace Vacanam.Infrastructure.Stubs;

public sealed class NullAudioRecorder : IAudioRecorder
{
    public event EventHandler<AudioDataEventArgs>? DataAvailable;

    public double AudioLevel => 0;
    public bool IsMuted { get; set; } = false;
    public float MasterVolume { get; set; } = 1.0f;
    public bool IsRecording => false;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public IReadOnlyList<AudioDevice> GetAvailableDevices() => Array.Empty<AudioDevice>();

    public void Dispose() { }
}