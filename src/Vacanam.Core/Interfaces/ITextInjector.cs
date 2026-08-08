namespace Vacanam.Core.Interfaces;

/// <summary>
/// Injects text into the currently focused application using the most
/// appropriate strategy (clipboard, SendInput, or UI Automation).
/// </summary>
public interface ITextInjector
{
    /// <summary>
    /// Injects the given text into the target window.
    /// Selects injection strategy automatically based on the application context.
    /// </summary>
    Task InjectAsync(
        string text,
        Models.ApplicationContext context,
        CancellationToken cancellationToken = default);
}
