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
    /// Simulates Ctrl + C to copy the currently selected text in the active window.
    /// Crucially: releases any physically held modifier keys (Shift, Alt, Win) before sending Ctrl+C,
    /// so that hotkeys like Shift+Space do not cause the OS to synthesize Ctrl+Shift+C (which copies format
    /// in Word/Outlook or opens external terminal in VS Code).
    /// If in push-to-talk mode, restores the logical Shift state afterward.
    /// </summary>
    public static async Task CopySelectionAsync(IntPtr hwnd = default, bool isPushToTalk = false, CancellationToken cancellationToken = default)
    {
        EnsureForeground(hwnd);
        await Task.Delay(15, cancellationToken);

        // 1. Detect currently held modifier keys
        bool shiftDown = (Win32Interop.GetAsyncKeyState(Win32Interop.VK_SHIFT) & 0x8000) != 0 ||
                         (Win32Interop.GetAsyncKeyState(0xA0) & 0x8000) != 0 || // VK_LSHIFT
                         (Win32Interop.GetAsyncKeyState(0xA1) & 0x8000) != 0;   // VK_RSHIFT
        bool altDown = (Win32Interop.GetAsyncKeyState(Win32Interop.VK_MENU) & 0x8000) != 0 ||
                       (Win32Interop.GetAsyncKeyState(0xA4) & 0x8000) != 0 || // VK_LMENU
                       (Win32Interop.GetAsyncKeyState(0xA5) & 0x8000) != 0;   // VK_RMENU
        bool winDown = (Win32Interop.GetAsyncKeyState(0x5B) & 0x8000) != 0 || // VK_LWIN
                       (Win32Interop.GetAsyncKeyState(0x5C) & 0x8000) != 0;   // VK_RWIN

        // 2. Release conflicting modifiers
        var releaseInputs = new List<INPUT>();
        if (shiftDown)
        {
            releaseInputs.Add(CreateKeyInput(Win32Interop.VK_SHIFT, KeyboardFlags.KEYEVENTF_KEYUP));
        }
        if (altDown)
        {
            releaseInputs.Add(CreateKeyInput(Win32Interop.VK_MENU, KeyboardFlags.KEYEVENTF_KEYUP));
        }
        if (winDown)
        {
            releaseInputs.Add(CreateKeyInput(0x5B, KeyboardFlags.KEYEVENTF_KEYUP));
            releaseInputs.Add(CreateKeyInput(0x5C, KeyboardFlags.KEYEVENTF_KEYUP));
        }

        if (releaseInputs.Count > 0)
        {
            Win32Interop.SendInput((uint)releaseInputs.Count, releaseInputs.ToArray(), INPUT.Size);
            await Task.Delay(20, cancellationToken);
        }

        // 3. Send Ctrl Down -> C Down -> C Up -> Ctrl Up
        var ctrlDown = new[] { CreateKeyInput(Win32Interop.VK_CONTROL, 0) };
        Win32Interop.SendInput(1, ctrlDown, INPUT.Size);
        await Task.Delay(15, cancellationToken);

        var cPress = new[]
        {
            CreateKeyInput(Win32Interop.VK_C, 0),
            CreateKeyInput(Win32Interop.VK_C, KeyboardFlags.KEYEVENTF_KEYUP)
        };
        Win32Interop.SendInput(2, cPress, INPUT.Size);
        await Task.Delay(15, cancellationToken);

        var ctrlUp = new[] { CreateKeyInput(Win32Interop.VK_CONTROL, KeyboardFlags.KEYEVENTF_KEYUP) };
        Win32Interop.SendInput(1, ctrlUp, INPUT.Size);
        await Task.Delay(15, cancellationToken);

        // 4. Restore Shift if the user is holding it for push-to-talk
        if (isPushToTalk && shiftDown)
        {
            var restoreInputs = new[] { CreateKeyInput(Win32Interop.VK_SHIFT, 0) };
            Win32Interop.SendInput(1, restoreInputs, INPUT.Size);
        }
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
