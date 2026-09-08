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
    private const int DictationHotkeyId = 0xB001;
    private const int AiTransformHotkeyId = 0xB002;

    private readonly ILogger<GlobalHotkeyService> _logger;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;

    private HotkeyMessageWindow? _messageWindow;
    private DispatcherTimer? _holdPollTimer;
    private bool _isKeyCurrentlyHeld = false;
    private int _suppressHoldDetectionCount = 0;
    private bool _disposed = false;

    // Active hold-tracking state
    private int _currentlyHeldHotkeyId;
    private HotkeyModifiers _currentlyHeldModifiers;
    private uint _currentlyHeldVirtualKey;

    // Cached resolved values from settings
    private HotkeyModifiers _modifiers;
    private uint _virtualKey;
    private HotkeyModifiers _aiTransformModifiers;
    private uint _aiTransformVirtualKey;

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler? AiTransformHotkeyPressed;
    public event EventHandler? AiTransformHotkeyReleased;

    public bool IsRegistered { get; private set; }
    public bool IsAiTransformRegistered { get; private set; }

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
        return _dispatcher.Invoke(() =>
        {
            try
            {
                EnsureMessageWindow();
                var hwnd = _messageWindow!.Handle;

                // 1. Register Dictation Hotkey (default Ctrl+Space)
                if (!IsRegistered)
                {
                    var modifiers = (uint)(_modifiers | HotkeyModifiers.NoRepeat);
                    bool ok = Win32Interop.RegisterHotKey(hwnd, DictationHotkeyId, modifiers, _virtualKey);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == Win32Interop.ERROR_HOTKEY_ALREADY_REGISTERED)
                        {
                            _logger.LogWarning("Dictation hotkey is already registered by another application.");
                        }
                        else
                        {
                            _logger.LogError("RegisterHotKey for dictation failed with Win32 error {Error}.", err);
                        }
                    }
                    else
                    {
                        IsRegistered = true;
                        _logger.LogInformation(
                            "Global dictation hotkey registered: modifiers={Modifiers:X}, vk={VK:X2}.",
                            modifiers, _virtualKey);
                    }
                }

                // 2. Register 'Ask AI' / Voice Transform Hotkey (default Shift+Space)
                if (!IsAiTransformRegistered && _settings.Hotkeys.EnableAiTransformHotkey)
                {
                    var aiModifiers = (uint)(_aiTransformModifiers | HotkeyModifiers.NoRepeat);
                    bool aiOk = Win32Interop.RegisterHotKey(hwnd, AiTransformHotkeyId, aiModifiers, _aiTransformVirtualKey);
                    if (!aiOk)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == Win32Interop.ERROR_HOTKEY_ALREADY_REGISTERED)
                        {
                            _logger.LogWarning("AI Transform hotkey is already registered by another application.");
                        }
                        else
                        {
                            _logger.LogError("RegisterHotKey for AI Transform failed with Win32 error {Error}.", err);
                        }
                    }
                    else
                    {
                        IsAiTransformRegistered = true;
                        _logger.LogInformation(
                            "Global AI Transform hotkey registered: modifiers={Modifiers:X}, vk={VK:X2}.",
                            aiModifiers, _aiTransformVirtualKey);
                    }
                }

                return IsRegistered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while registering global hotkeys.");
                return false;
            }
        });
    }

    public void Unregister()
    {
        if (_messageWindow is null) return;

        _dispatcher.Invoke(() =>
        {
            StopHoldPoller();
            if (IsRegistered)
            {
                Win32Interop.UnregisterHotKey(_messageWindow.Handle, DictationHotkeyId);
                IsRegistered = false;
            }

            if (IsAiTransformRegistered)
            {
                Win32Interop.UnregisterHotKey(_messageWindow.Handle, AiTransformHotkeyId);
                IsAiTransformRegistered = false;
            }

            _logger.LogInformation("Global hotkeys unregistered.");
        });
    }

    public bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey)
    {
        Unregister();
        _modifiers = (HotkeyModifiers)modifiers;
        _virtualKey = (uint)virtualKey;
        return Register(windowHandle);
    }

    public bool UpdateAiTransformRegistration(nint windowHandle, int modifiers, int virtualKey)
    {
        Unregister();
        _aiTransformModifiers = (HotkeyModifiers)modifiers;
        _aiTransformVirtualKey = (uint)virtualKey;
        return Register(windowHandle);
    }

    public void SuppressHoldDetection(bool suppress)
    {
        if (suppress)
        {
            Interlocked.Increment(ref _suppressHoldDetectionCount);
            _logger.LogDebug("Hold detection suppressed (count={Count}).", _suppressHoldDetectionCount);
        }
        else
        {
            Interlocked.Decrement(ref _suppressHoldDetectionCount);
            _logger.LogDebug("Hold detection resumed (count={Count}).", _suppressHoldDetectionCount);
        }
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
        if (hotkeyId != DictationHotkeyId && hotkeyId != AiTransformHotkeyId) return;

        bool isDictation = hotkeyId == DictationHotkeyId;
        var targetModifiers = isDictation ? _modifiers : _aiTransformModifiers;
        var targetVk = isDictation ? _virtualKey : _aiTransformVirtualKey;

        if (_settings.Hotkeys.PushToTalk)
        {
            // Push-to-talk: key press -> start recording
            if (!_isKeyCurrentlyHeld)
            {
                _isKeyCurrentlyHeld = true;
                _currentlyHeldHotkeyId = hotkeyId;
                _currentlyHeldModifiers = targetModifiers;
                _currentlyHeldVirtualKey = targetVk;

                _logger.LogDebug("Hotkey {Id:X} pressed (push-to-talk start).", hotkeyId);
                StartHoldPoller();
                if (isDictation)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    AiTransformHotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        else
        {
            // Toggle mode: first press -> start, second press -> stop
            if (!_isKeyCurrentlyHeld)
            {
                _isKeyCurrentlyHeld = true;
                _currentlyHeldHotkeyId = hotkeyId;
                _currentlyHeldModifiers = targetModifiers;
                _currentlyHeldVirtualKey = targetVk;

                _logger.LogDebug("Hotkey {Id:X} pressed (toggle: start).", hotkeyId);
                if (isDictation)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    AiTransformHotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _isKeyCurrentlyHeld = false;
                int prevHeld = _currentlyHeldHotkeyId;
                _logger.LogDebug("Hotkey {Id:X} pressed (toggle: stop).", hotkeyId);
                if (prevHeld == DictationHotkeyId)
                {
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    AiTransformHotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
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
        if (!_isKeyCurrentlyHeld || _suppressHoldDetectionCount > 0) return;

        bool modHeld = true;
        if (_currentlyHeldModifiers.HasFlag(HotkeyModifiers.Ctrl))
        {
            modHeld &= (Win32Interop.GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
        }
        if (_currentlyHeldModifiers.HasFlag(HotkeyModifiers.Shift))
        {
            modHeld &= (Win32Interop.GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
        }
        if (_currentlyHeldModifiers.HasFlag(HotkeyModifiers.Alt))
        {
            modHeld &= (Win32Interop.GetAsyncKeyState(0x12) & 0x8000) != 0; // VK_MENU
        }
        if (_currentlyHeldModifiers.HasFlag(HotkeyModifiers.Win))
        {
            modHeld &= ((Win32Interop.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (Win32Interop.GetAsyncKeyState(0x5C) & 0x8000) != 0); // VK_LWIN / VK_RWIN
        }

        short keyState = Win32Interop.GetAsyncKeyState((int)_currentlyHeldVirtualKey);
        bool keyHeld = (keyState & 0x8000) != 0;

        bool stillHeld = modHeld && keyHeld;

        if (!stillHeld)
        {
            _isKeyCurrentlyHeld = false;
            int releasedId = _currentlyHeldHotkeyId;
            StopHoldPoller();
            _logger.LogDebug("Hotkey {Id:X} released (push-to-talk stop).", releasedId);
            if (releasedId == DictationHotkeyId)
            {
                HotkeyReleased?.Invoke(this, EventArgs.Empty);
            }
            else if (releasedId == AiTransformHotkeyId)
            {
                AiTransformHotkeyReleased?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // -- Helpers ---------------------------------------------------------------

    private void ResolveKeyFromSettings()
    {
        _modifiers = (HotkeyModifiers)_settings.Hotkeys.Modifiers;
        _virtualKey = (uint)_settings.Hotkeys.VirtualKey;
        _aiTransformModifiers = (HotkeyModifiers)_settings.Hotkeys.AiTransformModifiers;
        _aiTransformVirtualKey = (uint)_settings.Hotkeys.AiTransformVirtualKey;

        _logger.LogDebug(
            "Hotkeys resolved from settings: Dictation(mod={Mod}, vk=0x{VK:X2}), AiTransform(mod={AiMod}, vk=0x{AiVK:X2})",
            _modifiers, _virtualKey, _aiTransformModifiers, _aiTransformVirtualKey);
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
