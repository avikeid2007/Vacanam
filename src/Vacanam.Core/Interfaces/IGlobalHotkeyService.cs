namespace Vacanam.Core.Interfaces;

/// <summary>
/// Registers and manages the global push-to-talk / toggle hotkey.
/// Must operate without stealing focus from the target application.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>Raised when the hotkey is pressed (push-to-talk: hold start; toggle: first press).</summary>
    event EventHandler HotkeyPressed;

    /// <summary>Raised when the hotkey is released (push-to-talk only).</summary>
    event EventHandler HotkeyReleased;

    /// <summary>True if the hotkey is currently registered with the OS.</summary>
    bool IsRegistered { get; }

    /// <summary>Registers the hotkey defined in settings. Returns false if the combination is already taken.</summary>
    bool Register(nint windowHandle);

    /// <summary>Unregisters the hotkey. Safe to call even if not registered.</summary>
    void Unregister();

    /// <summary>Re-registers with updated key combination from settings.</summary>
    bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey);
}
