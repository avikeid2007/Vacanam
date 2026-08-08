namespace Vacanam.Core.Interfaces;

/// <summary>
/// Provides information about the currently active (foreground) Windows application.
/// </summary>
public interface IForegroundWindowService
{
    /// <summary>
    /// Captures a snapshot of the current foreground window.
    /// Returns ApplicationContext.Unknown if capture fails.
    /// </summary>
    Models.ApplicationContext GetCurrentContext();
}
