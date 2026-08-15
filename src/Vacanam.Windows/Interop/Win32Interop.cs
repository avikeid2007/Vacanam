using System.Runtime.InteropServices;

namespace Vacanam.Windows.Interop;

/// <summary>
/// Centralised P/Invoke declarations for Win32 APIs used by Vacanam.
/// ALL unmanaged calls are declared here.
/// </summary>
public static class Win32Interop
{
    // -- user32.dll ------------------------------------------------------------

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LockWorkStation();

    // -- Message constants -----------------------------------------------------

    public const int WM_HOTKEY = 0x0312;

    // -- Virtual key codes -----------------------------------------------------

    public const ushort VK_BACK = 0x08;
    public const ushort VK_TAB = 0x09;
    public const ushort VK_RETURN = 0x0D;
    public const ushort VK_SHIFT = 0x10;
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_MENU = 0x12; // Alt
    public const ushort VK_ESCAPE = 0x1B;
    public const uint VK_SPACE = 0x20;
    public const ushort VK_END = 0x23;
    public const ushort VK_HOME = 0x24;
    public const ushort VK_DELETE = 0x2E;

    public const ushort VK_A = 0x41;
    public const ushort VK_C = 0x43;
    public const ushort VK_S = 0x53;
    public const ushort VK_V = 0x56;
    public const ushort VK_X = 0x58;
    public const ushort VK_Y = 0x59;
    public const ushort VK_Z = 0x5A;
    public const ushort VK_F4 = 0x73;

    // -- Error codes -----------------------------------------------------------

    public const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;
}

// -- SendInput Structures ------------------------------------------------------

[StructLayout(LayoutKind.Sequential)]
public struct INPUT
{
    public uint type;
    public InputUnion U;

    public static int Size => Marshal.SizeOf(typeof(INPUT));
}

[StructLayout(LayoutKind.Explicit)]
public struct InputUnion
{
    [FieldOffset(0)]
    public MOUSEINPUT mi;
    [FieldOffset(0)]
    public KEYBDINPUT ki;
    [FieldOffset(0)]
    public HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct HARDWAREINPUT
{
    public uint uMsg;
    public ushort wParamL;
    public ushort wParamH;
}

public static class InputTypes
{
    public const uint INPUT_KEYBOARD = 1;
}

public static class KeyboardFlags
{
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0400;
    public const uint KEYEVENTF_SCANCODE = 0x0004;
}

/// <summary>
/// Win32 modifier key flags for RegisterHotKey.
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None  = 0x0000,
    Alt   = 0x0001,
    Ctrl  = 0x0002,
    Shift = 0x0004,
    Win   = 0x0008,
    NoRepeat = 0x4000,
}
