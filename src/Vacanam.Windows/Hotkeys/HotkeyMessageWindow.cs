using System.Windows;
using System.Windows.Interop;
using Vacanam.Windows.Interop;

namespace Vacanam.Windows.Hotkeys;

/// <summary>
/// An invisible WPF window whose sole purpose is to own a Win32 HWND message pump
/// so that WM_HOTKEY messages can be received via HwndSource.
///
/// This window is never shown, never appears in the taskbar, and captures no input.
/// It is created on the WPF UI thread and lives for the entire application lifetime.
/// </summary>
internal sealed class HotkeyMessageWindow : Window
{
    private HwndSource? _hwndSource;

    /// <summary>The HWND of this hidden window, used with RegisterHotKey.</summary>
    public IntPtr Handle { get; private set; }

    /// <summary>Raised when a WM_HOTKEY message is received for the registered hotkey ID.</summary>
    public event EventHandler<int>? HotkeyReceived; // arg = hotkey ID

    public HotkeyMessageWindow()
    {
        // Make window completely invisible and out of the way
        Width = 0;
        Height = 0;
        WindowStyle = WindowStyle.None;
        ShowInTaskbar = false;
        ShowActivated = false;
        Visibility = Visibility.Hidden;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        Opacity = 0;

        // Get the HWND after the source is initialised
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        Handle = _hwndSource?.Handle ?? IntPtr.Zero;
        _hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// WPF message hook — intercepts WM_HOTKEY and raises HotkeyReceived.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_HOTKEY)
        {
            var hotkeyId = wParam.ToInt32();
            HotkeyReceived?.Invoke(this, hotkeyId);
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        base.OnClosed(e);
    }
}
