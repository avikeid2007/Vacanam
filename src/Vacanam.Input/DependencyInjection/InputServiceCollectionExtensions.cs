using Microsoft.Extensions.DependencyInjection;
using Vacanam.Core.Interfaces;
using Vacanam.Input.Services;
using Vacanam.Input.Strategies;

namespace Vacanam.Input.DependencyInjection;

/// <summary>
/// Extension methods for registering Phase 5 text injection services.
/// </summary>
public static class InputServiceCollectionExtensions
{
    /// <summary>
    /// Registers text injection services and strategies into DI container.
    /// Replaces: NullClipboardService -> WindowsClipboardService
    ///           NullTextInjector -> CompositeTextInjector
    /// </summary>
    public static IServiceCollection AddVacanamInputServices(this IServiceCollection services)
    {
        services.AddSingleton<IClipboardService, WindowsClipboardService>();

        services.AddSingleton<ClipboardTextInjector>();
        services.AddSingleton<SendInputTextInjector>();
        services.AddSingleton<UiAutomationTextInjector>();

        services.AddSingleton<ITextInjector, CompositeTextInjector>();

        return services;
    }
}
