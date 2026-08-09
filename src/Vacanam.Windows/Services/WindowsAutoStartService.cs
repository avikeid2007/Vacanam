using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;

namespace Vacanam.Windows.Services;

/// <summary>
/// Native Windows Registry implementation for managing "Start with Windows" auto-launch.
/// Reads and writes to HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// </summary>
public sealed class WindowsAutoStartService : IAutoStartService
{
    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Vacanam";
    private readonly ILogger<WindowsAutoStartService> _logger;

    public WindowsAutoStartService(ILogger<WindowsAutoStartService> logger)
    {
        _logger = logger;
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Windows Auto-Start registry key.");
            return false;
        }
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
            if (key is null)
            {
                _logger.LogWarning("Windows Registry Run key path could not be opened for writing.");
                return;
            }

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    _logger.LogInformation("Enabled Vacanam Windows startup registry entry: {Path}", exePath);
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                _logger.LogInformation("Disabled Vacanam Windows startup registry entry.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Windows Auto-Start registry entry.");
        }
    }
}
