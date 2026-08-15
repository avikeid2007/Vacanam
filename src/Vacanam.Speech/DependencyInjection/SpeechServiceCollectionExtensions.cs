using Microsoft.Extensions.DependencyInjection;
using Vacanam.Core.Interfaces;
using Vacanam.Speech.Commands;
using Vacanam.Speech.Model;
using Vacanam.Speech.Punctuation;
using Vacanam.Speech.Recognition;

namespace Vacanam.Speech.DependencyInjection;

/// <summary>
/// Extension methods for registering Phase 4 speech recognition services and Phase 7 voice commands.
/// </summary>
public static class SpeechServiceCollectionExtensions
{
    /// <summary>
    /// Registers Whisper speech recognition, Smart Punctuation, and Voice Command services.
    /// </summary>
    public static IServiceCollection AddVacanamSpeechServices(this IServiceCollection services)
    {
        services.AddSingleton<ModelManager>();
        services.AddSingleton<IModelManager>(sp => sp.GetRequiredService<ModelManager>());

        services.AddSingleton<ISpeechRecognizer, WhisperSpeechRecognizer>();

        services.AddSingleton<SmartPunctuationProcessor>();
        services.AddSingleton<IVoiceCommandProcessor, VoiceCommandProcessor>();

        return services;
    }
}
