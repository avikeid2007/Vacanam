using Microsoft.Extensions.DependencyInjection;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Windows.ForegroundWindow;
using Vacanam.Windows.Hotkeys;

namespace Vacanam.Windows.DependencyInjection;

/// <summary>
/// Extension methods that register the Phase 2 Win32 service implementations.
/// Call this from App.xaml.cs after AddVacanamServices() to replace Phase 1 stubs.
/// </summary>
public static class WindowsServiceCollectionExtensions
{
    /// <summary>
    /// Registers production Windows-native service implementations.
    /// Replaces the Phase 1 null stubs for hotkeys and foreground-window detection.
    /// </summary>
    public static IServiceCollection AddVacanamWindowsServices(this IServiceCollection services)
    {
        // Phase 2: Replace NullHotkeyService ? GlobalHotkeyService
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();

        // Phase 2: Replace NullForegroundWindowService ? WindowsForegroundWindowService
        services.AddSingleton<IForegroundWindowService, WindowsForegroundWindowService>();

        return services;
    }
}
