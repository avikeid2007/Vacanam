using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Windows.Interop;

namespace Vacanam.Input.Strategies;

/// <summary>
/// Primary text injection strategy using Clipboard + Ctrl+V.
/// Tuned for ultra-fast, low-latency execution (<30ms end-to-end).
/// </summary>
public sealed class ClipboardTextInjector : ITextInjector
{
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<ClipboardTextInjector> _logger;

    public ClipboardTextInjector(
        IClipboardService clipboardService,
        ILogger<ClipboardTextInjector> logger)
    {
        _clipboardService = clipboardService;
        _logger = logger;
    }

    public async Task InjectAsync(string text, ApplicationContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return;

        _logger.LogInformation("Injecting {Len} chars via Clipboard + Ctrl+V to HWND={Hwnd:X} ({Process})",
            text.Length, context.WindowHandle, context.ProcessName);

        if (context.WindowHandle != IntPtr.Zero)
        {
            Win32Interop.SetForegroundWindow(context.WindowHandle);
        }

        // Backup existing clipboard
        object? backup = null;
        try
        {
            backup = await _clipboardService.BackupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to backup clipboard prior to injection.");
        }

        try
        {
            // Set text on clipboard
            await _clipboardService.SetTextAsync(text);

            // Synthesize Ctrl+V keypress
            SendCtrlV();

            // Delay for target application to process paste message before clipboard restoration
            await Task.Delay(120, cancellationToken);
        }
        finally
        {
            // Restore original clipboard contents
            if (backup is not null)
            {
                try
                {
                    await _clipboardService.RestoreAsync(backup);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore clipboard backup after injection.");
                }
            }
        }
    }

    private static void SendCtrlV()
    {
        // Release conflicting modifiers (Shift, Alt, Win) so Ctrl+V is not corrupted into Ctrl+Shift+V
        bool shiftDown = (Win32Interop.GetAsyncKeyState(Win32Interop.VK_SHIFT) & 0x8000) != 0;
        bool altDown = (Win32Interop.GetAsyncKeyState(Win32Interop.VK_MENU) & 0x8000) != 0;
        bool winDown = (Win32Interop.GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
                       (Win32Interop.GetAsyncKeyState(0x5C) & 0x8000) != 0;

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
        }

        var inputs = new INPUT[4];

        // 1. Ctrl Down
        inputs[0] = CreateKeyInput(Win32Interop.VK_CONTROL, 0);

        // 2. V Down
        inputs[1] = CreateKeyInput(Win32Interop.VK_V, 0);

        // 3. V Up
        inputs[2] = CreateKeyInput(Win32Interop.VK_V, KeyboardFlags.KEYEVENTF_KEYUP);

        // 4. Ctrl Up
        inputs[3] = CreateKeyInput(Win32Interop.VK_CONTROL, KeyboardFlags.KEYEVENTF_KEYUP);

        Win32Interop.SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }

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
}
