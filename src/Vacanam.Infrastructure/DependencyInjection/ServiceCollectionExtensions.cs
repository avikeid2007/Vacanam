using Microsoft.Extensions.DependencyInjection;
using Vacanam.Core.Interfaces;
using Vacanam.Infrastructure.Configuration;
using Vacanam.Infrastructure.Stubs;

namespace Vacanam.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for registering Vacanam services with the DI container.
///
/// Phase 1 registers null/stub implementations for interfaces that will be
/// replaced in subsequent phases by calling phase-specific registration methods
/// (e.g., AddVacanamWindowsServices() for Phase 2).
///
/// Replacement pattern: later AddSingleton calls override earlier ones —
/// the last registration wins when resolving via GetRequiredService.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Phase 1 stub implementations.
    /// These are the fallback null-objects for interfaces not yet implemented.
    /// Phase-specific packages override individual entries via their own extension methods.
    /// </summary>
    public static IServiceCollection AddVacanamServices(this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<SettingsManager>();

        // Phase 1 stubs — will be overridden by phase-specific registrations:
        //   Phase 2 ? Vacanam.Windows overrides IGlobalHotkeyService + IForegroundWindowService
        //   Phase 3 ? Vacanam.Audio  overrides IAudioRecorder
        //   Phase 4 ? Vacanam.Speech overrides ISpeechRecognizer
        //   Phase 5 ? Vacanam.Input  overrides ITextInjector
        services.AddSingleton<IGlobalHotkeyService,    NullHotkeyService>();
        services.AddSingleton<IForegroundWindowService, NullForegroundWindowService>();
        services.AddSingleton<IAudioRecorder,           NullAudioRecorder>();
        services.AddSingleton<ISpeechRecognizer,        NullSpeechRecognizer>();
        services.AddSingleton<ITextInjector,            NullTextInjector>();

        return services;
    }

    /// <summary>
    /// Ensures required local data directories exist (Models, Logs, Data).
    /// Safe to call multiple times — Directory.CreateDirectory is idempotent.
    /// </summary>
    public static IServiceCollection AddVacanamDirectories(this IServiceCollection services)
    {
        var appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vacanam");

        var directories = new[]
        {
            appDataRoot,
            Path.Combine(appDataRoot, "Models", "Whisper"),
            Path.Combine(appDataRoot, "Models", "LLM"),
            Path.Combine(appDataRoot, "Logs"),
            Path.Combine(appDataRoot, "Data"),
        };

        foreach (var dir in directories)
            Directory.CreateDirectory(dir);

        return services;
    }
}
