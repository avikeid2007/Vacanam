using Microsoft.Extensions.Logging;

namespace Vacanam.Windows.Interop;

/// <summary>
/// Native keyboard shortcut and keystroke synthesizer using Win32 SendInput.
/// Used for voice action command execution (e.g. "select all", "undo", "press enter").
/// </summary>
public static class KeySimulator
{
    /// <summary>
    /// Sends a single virtual key (Key Down -> Key Up).
    /// </summary>
    public static async Task SendKeyAsync(ushort vk, IntPtr hwnd = default, CancellationToken cancellationToken = default)
    {
        EnsureForeground(hwnd);
        await Task.Delay(20, cancellationToken);

        var inputs = new INPUT[]
        {
            CreateKeyInput(vk, 0),
            CreateKeyInput(vk, KeyboardFlags.KEYEVENTF_KEYUP)
        };

        Win32Interop.SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }

    /// <summary>
    /// Sends a modified keystroke (Modifier Down -> Key Down -> Key Up -> Modifier Up).
    /// Example: Ctrl + A, Ctrl + Z, Alt + F4.
    /// </summary>
    public static async Task SendModifiedKeyAsync(ushort modifierVk, ushort keyVk, IntPtr hwnd = default, CancellationToken cancellationToken = default)
    {
        EnsureForeground(hwnd);
        await Task.Delay(20, cancellationToken);

        var inputs = new INPUT[]
        {
            CreateKeyInput(modifierVk, 0),
            CreateKeyInput(keyVk, 0),
            CreateKeyInput(keyVk, KeyboardFlags.KEYEVENTF_KEYUP),
            CreateKeyInput(modifierVk, KeyboardFlags.KEYEVENTF_KEYUP)
        };

        Win32Interop.SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }

    /// <summary>
    /// Sends a sequence of modified keys to select and delete the current line (Shift+Home, then Delete).
    /// </summary>
    public static async Task DeleteLineAsync(IntPtr hwnd = default, CancellationToken cancellationToken = default)
    {
        EnsureForeground(hwnd);
        await Task.Delay(20, cancellationToken);

        // 1. Shift + Home (Select to start of line)
        var selectInputs = new INPUT[]
        {
            CreateKeyInput(Win32Interop.VK_SHIFT, 0),
            CreateKeyInput(Win32Interop.VK_HOME, KeyboardFlags.KEYEVENTF_EXTENDEDKEY),
            CreateKeyInput(Win32Interop.VK_HOME, KeyboardFlags.KEYEVENTF_EXTENDEDKEY | KeyboardFlags.KEYEVENTF_KEYUP),
            CreateKeyInput(Win32Interop.VK_SHIFT, KeyboardFlags.KEYEVENTF_KEYUP)
        };
        Win32Interop.SendInput((uint)selectInputs.Length, selectInputs, INPUT.Size);

        await Task.Delay(25, cancellationToken);

        // 2. Delete
        var deleteInputs = new INPUT[]
        {
            CreateKeyInput(Win32Interop.VK_DELETE, KeyboardFlags.KEYEVENTF_EXTENDEDKEY),
            CreateKeyInput(Win32Interop.VK_DELETE, KeyboardFlags.KEYEVENTF_EXTENDEDKEY | KeyboardFlags.KEYEVENTF_KEYUP)
        };
        Win32Interop.SendInput((uint)deleteInputs.Length, deleteInputs, INPUT.Size);
    }

    /// <summary>
    /// Creates an INPUT structure configured for keyboard input.
    /// </summary>
    private static INPUT CreateKeyInput(ushort vk, uint flags)
    {
        return new INPUT
        {
            type = InputTypes.INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static void EnsureForeground(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero && hwnd != Win32Interop.GetForegroundWindow())
        {
            Win32Interop.SetForegroundWindow(hwnd);
        }
    }
}
