using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Vacanam.Core.Exceptions;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Windows.Interop;

namespace Vacanam.Windows.Hotkeys;

/// <summary>
/// Production implementation of IGlobalHotkeyService using Win32 RegisterHotKey.
///
/// Design decisions:
/// - Uses a hidden HotkeyMessageWindow to own the HWND and process WM_HOTKEY.
/// - Does NOT use SetWindowsHookEx (global keyboard hook) — RegisterHotKey is sufficient
///   and requires no elevated privileges for standard user-level keys.
/// - Push-to-talk "hold" detection: WM_HOTKEY fires on press. Key-up is detected by
///   polling GetAsyncKeyState on a background timer (lightweight, ~16ms interval).
/// - MOD_NOREPEAT (0x4000) prevents repeated WM_HOTKEY while key is held, so we get
///   exactly one press event and poll for the release.
/// </summary>
public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int HotkeyId = 0xB00B; // Application-unique ID

    private readonly ILogger<GlobalHotkeyService> _logger;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;

    private HotkeyMessageWindow? _messageWindow;
    private DispatcherTimer? _holdPollTimer;
    private bool _isKeyCurrentlyHeld = false;
    private bool _disposed = false;

    // Cached resolved values from settings
    private HotkeyModifiers _modifiers;
    private uint _virtualKey;

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public bool IsRegistered { get; private set; }

    public GlobalHotkeyService(
        IOptions<AppSettings> settings,
        ILogger<GlobalHotkeyService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _dispatcher = Application.Current.Dispatcher;
        ResolveKeyFromSettings();
    }

    // -- IGlobalHotkeyService --------------------------------------------------

    public bool Register(nint windowHandle)
    {
        if (IsRegistered) return true;

        return _dispatcher.Invoke(() =>
        {
            try
            {
                EnsureMessageWindow();

                var hwnd = _messageWindow!.Handle;
                var modifiers = (uint)(_modifiers | HotkeyModifiers.NoRepeat);

                bool ok = Win32Interop.RegisterHotKey(hwnd, HotkeyId, modifiers, _virtualKey);
                if (!ok)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err == Win32Interop.ERROR_HOTKEY_ALREADY_REGISTERED)
                    {
                        _logger.LogWarning(
                            "Hotkey Ctrl+Space is already registered by another application. " +
                            "You can change the hotkey in Settings ? Hotkeys.");
                    }
                    else
                    {
                        _logger.LogError("RegisterHotKey failed with Win32 error {Error}.", err);
                    }
                    return false;
                }

                IsRegistered = true;
                _logger.LogInformation(
                    "Global hotkey registered: modifiers={Modifiers:X}, vk={VK:X2} (Ctrl+Space).",
                    modifiers, _virtualKey);

                // Start hold-polling timer for push-to-talk mode
                if (_settings.Hotkeys.PushToTalk)
                    StartHoldPoller();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while registering global hotkey.");
                return false;
            }
        });
    }

    public void Unregister()
    {
        if (!IsRegistered || _messageWindow is null) return;

        _dispatcher.Invoke(() =>
        {
            StopHoldPoller();
            Win32Interop.UnregisterHotKey(_messageWindow.Handle, HotkeyId);
            IsRegistered = false;
            _logger.LogInformation("Global hotkey unregistered.");
        });
    }

    public bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey)
    {
        Unregister();
        _modifiers = (HotkeyModifiers)modifiers;
        _virtualKey = (uint)virtualKey;
        return Register(windowHandle);
    }

    // -- Internal --------------------------------------------------------------

    private void EnsureMessageWindow()
    {
        if (_messageWindow is not null) return;

        _messageWindow = new HotkeyMessageWindow();
        _messageWindow.Show(); // Must call Show() to initialise SourceInitialized
        _messageWindow.Hide(); // Immediately hide — window is invisible to user
        _messageWindow.HotkeyReceived += OnHotkeyReceived;

        _logger.LogDebug("HotkeyMessageWindow created. HWND={Handle:X}", _messageWindow.Handle);
    }

    private void OnHotkeyReceived(object? sender, int hotkeyId)
    {
        if (hotkeyId != HotkeyId) return;

        if (_settings.Hotkeys.PushToTalk)
        {
            // Push-to-talk: key press ? start recording
            if (!_isKeyCurrentlyHeld)
            {
                _isKeyCurrentlyHeld = true;
                _logger.LogDebug("Hotkey pressed (push-to-talk start).");
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            // Toggle mode: first press ? start, second press ? stop
            if (!_isKeyCurrentlyHeld)
            {
                _isKeyCurrentlyHeld = true;
                _logger.LogDebug("Hotkey pressed (toggle: start).");
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _isKeyCurrentlyHeld = false;
                _logger.LogDebug("Hotkey pressed (toggle: stop).");
                HotkeyReleased?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // -- Hold Poller (Push-to-talk key-up detection) ------------------------

    private void StartHoldPoller()
    {
        _holdPollTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60Hz polling
        };
        _holdPollTimer.Tick += PollKeyRelease;
        _holdPollTimer.Start();
        _logger.LogDebug("Hold-poller started (16ms interval).");
    }

    private void StopHoldPoller()
    {
        if (_holdPollTimer is null) return;
        _holdPollTimer.Stop();
        _holdPollTimer.Tick -= PollKeyRelease;
        _holdPollTimer = null;
        _logger.LogDebug("Hold-poller stopped.");
    }

    private void PollKeyRelease(object? sender, EventArgs e)
    {
        if (!_isKeyCurrentlyHeld) return;

        // GetAsyncKeyState returns negative if the key is currently down
        short ctrlState  = Win32Interop.GetAsyncKeyState(0x11); // VK_CONTROL
        short spaceState = Win32Interop.GetAsyncKeyState((int)_virtualKey);

        bool ctrlDown  = (ctrlState  & 0x8000) != 0;
        bool spaceDown = (spaceState & 0x8000) != 0;

        // Both modifier AND key must still be held — release when either is lifted
        bool stillHeld = _modifiers.HasFlag(HotkeyModifiers.Ctrl)
            ? ctrlDown && spaceDown
            : spaceDown;

        if (!stillHeld)
        {
            _isKeyCurrentlyHeld = false;
            _logger.LogDebug("Hotkey released (push-to-talk stop).");
            HotkeyReleased?.Invoke(this, EventArgs.Empty);
        }
    }

    // -- Helpers ---------------------------------------------------------------

    private void ResolveKeyFromSettings()
    {
        // Settings store modifiers as a bitmask (1=Alt, 2=Ctrl, 4=Shift, 8=Win)
        // matching HotkeyModifiers values exactly
        _modifiers = (HotkeyModifiers)_settings.Hotkeys.Modifiers;
        _virtualKey = (uint)_settings.Hotkeys.VirtualKey;

        _logger.LogDebug(
            "Hotkey resolved from settings: modifiers={Modifiers}, vk=0x{VK:X2}",
            _modifiers, _virtualKey);
    }

    // -- IDisposable ------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dispatcher.Invoke(() =>
        {
            Unregister();
            if (_messageWindow is not null)
            {
                _messageWindow.HotkeyReceived -= OnHotkeyReceived;
                _messageWindow.Close();
                _messageWindow = null;
            }
        });

        _logger.LogDebug("GlobalHotkeyService disposed.");
    }
}
