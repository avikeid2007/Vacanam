using Microsoft.Extensions.DependencyInjection;
using Vacanam.Audio.Capture;
using Vacanam.Audio.Vad;
using Vacanam.Core.Interfaces;

namespace Vacanam.Audio.DependencyInjection;

/// <summary>
/// Extension methods registering Phase 3 audio services.
/// Call after AddVacanamServices() to replace the NullAudioRecorder stub.
/// </summary>
public static class AudioServiceCollectionExtensions
{
    /// <summary>
    /// Registers WASAPI audio capture and energy-based VAD.
    /// Replaces: NullAudioRecorder → WasapiAudioRecorder
    /// Adds:     IVoiceActivityDetector → EnergyVoiceActivityDetector
    ///           RecordingBuffer (transient — new instance per recording session)
    /// </summary>
    public static IServiceCollection AddVacanamAudioServices(this IServiceCollection services)
    {
        // Phase 3: replace NullAudioRecorder with real WASAPI implementation
        services.AddSingleton<IAudioRecorder, WasapiAudioRecorder>();

        // VAD — singleton (holds hysteresis state, reset between sessions)
        services.AddSingleton<IVoiceActivityDetector, EnergyVoiceActivityDetector>();

        // Recording buffer — transient: one per session, injected into ApplicationLifetimeService
        services.AddTransient<RecordingBuffer>();

        return services;
    }
}
