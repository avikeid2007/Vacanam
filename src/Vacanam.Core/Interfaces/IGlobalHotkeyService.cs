namespace Vacanam.Core.Interfaces;

/// <summary>
/// Registers and manages the global push-to-talk / toggle hotkey.
/// Must operate without stealing focus from the target application.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>Raised when the primary dictation hotkey is pressed.</summary>
    event EventHandler HotkeyPressed;

    /// <summary>Raised when the primary dictation hotkey is released.</summary>
    event EventHandler HotkeyReleased;

    /// <summary>Raised when the 'Ask AI' / Voice Transform hotkey is pressed.</summary>
    event EventHandler AiTransformHotkeyPressed;

    /// <summary>Raised when the 'Ask AI' / Voice Transform hotkey is released.</summary>
    event EventHandler AiTransformHotkeyReleased;

    /// <summary>True if the primary dictation hotkey is currently registered with the OS.</summary>
    bool IsRegistered { get; }

    /// <summary>True if the 'Ask AI' / Voice Transform hotkey is currently registered with the OS.</summary>
    bool IsAiTransformRegistered { get; }

    /// <summary>Registers hotkeys defined in settings. Returns false if combination is taken.</summary>
    bool Register(nint windowHandle);

    /// <summary>Unregisters all hotkeys. Safe to call even if not registered.</summary>
    void Unregister();

    /// <summary>Re-registers primary dictation hotkey with updated key combination.</summary>
    bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey);

    /// <summary>Re-registers 'Ask AI' / Voice Transform hotkey with updated key combination.</summary>
    bool UpdateAiTransformRegistration(nint windowHandle, int modifiers, int virtualKey);

    /// <summary>
    /// Temporarily suppresses push-to-talk key-up hold detection during simulated keystrokes
    /// (e.g. selection copy) to prevent race conditions when releasing modifier keys.
    /// </summary>
    void SuppressHoldDetection(bool suppress);
}
