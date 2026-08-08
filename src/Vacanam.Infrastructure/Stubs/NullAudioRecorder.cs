#pragma warning disable CS0067 // [MOCK] Event declared but not raised in stub implementation
using Vacanam.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vacanam.Infrastructure.Stubs;

/// <summary>
/// [MOCK] Null implementation of IAudioRecorder.
/// Used in Phase 1 (App Shell). Will be replaced by NAudio WASAPI implementation in Phase 3.
/// </summary>
internal sealed class NullAudioRecorder(ILogger<NullAudioRecorder> logger) : IAudioRecorder
{
    public event EventHandler<AudioDataEventArgs>? DataAvailable;
    public double AudioLevel => 0.0;
    public bool IsRecording => false;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogWarning("[MOCK] NullAudioRecorder.StartAsync called — no audio will be captured until Phase 3.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogWarning("[MOCK] NullAudioRecorder.StopAsync called.");
        return Task.CompletedTask;
    }

    public IReadOnlyList<AudioDevice> GetAvailableDevices() =>
        [new AudioDevice("null", "No microphone (Phase 1 stub)", true)];

    public void Dispose() { }
}

