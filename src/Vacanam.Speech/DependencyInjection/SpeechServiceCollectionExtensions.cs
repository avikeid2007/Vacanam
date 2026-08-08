using Microsoft.Extensions.DependencyInjection;
using Vacanam.Core.Interfaces;
using Vacanam.Speech.Model;
using Vacanam.Speech.Recognition;

namespace Vacanam.Speech.DependencyInjection;

/// <summary>
/// Extension methods for registering Phase 4 speech recognition services.
/// </summary>
public static class SpeechServiceCollectionExtensions
{
    /// <summary>
    /// Registers Whisper speech recognition and model management services.
    /// Replaces: NullSpeechRecognizer -> WhisperSpeechRecognizer
    /// Registers: ModelManager -> IModelManager
    /// </summary>
    public static IServiceCollection AddVacanamSpeechServices(this IServiceCollection services)
    {
        services.AddSingleton<ModelManager>();
        services.AddSingleton<IModelManager>(sp => sp.GetRequiredService<ModelManager>());

        services.AddSingleton<ISpeechRecognizer, WhisperSpeechRecognizer>();

        return services;
    }
}
