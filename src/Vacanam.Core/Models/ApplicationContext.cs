namespace Vacanam.Core.Models;

/// <summary>
/// Immutable snapshot of the foreground application at the moment recording starts.
/// Used to provide context to the LLM and to select the correct text injection strategy.
/// </summary>
/// <param name="WindowHandle">Handle to the foreground window (HWND).</param>
/// <param name="ProcessId">Win32 process ID of the foreground application.</param>
/// <param name="ProcessName">Executable name without extension (e.g. "chrome", "devenv").</param>
/// <param name="WindowTitle">Title bar text of the foreground window at capture time.</param>
public sealed record ApplicationContext(
    nint WindowHandle,
    int ProcessId,
    string ProcessName,
    string WindowTitle)
{
    /// <summary>A neutral unknown context used when detection fails or is unavailable.</summary>
    public static readonly ApplicationContext Unknown = new(0, 0, string.Empty, string.Empty);

    /// <summary>Returns true if the context represents a valid captured window.</summary>
    public bool IsValid => WindowHandle != 0 && ProcessId > 0;
}
