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

            // Minimal delay for target application to process paste message
            await Task.Delay(25, cancellationToken);
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
        var inputs = new INPUT[4];

        // 1. Ctrl Down
        inputs[0].type = InputTypes.INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = Win32Interop.VK_CONTROL;
        inputs[0].U.ki.dwFlags = 0;

        // 2. V Down
        inputs[1].type = InputTypes.INPUT_KEYBOARD;
        inputs[1].U.ki.wVk = Win32Interop.VK_V;
        inputs[1].U.ki.dwFlags = 0;

        // 3. V Up
        inputs[2].type = InputTypes.INPUT_KEYBOARD;
        inputs[2].U.ki.wVk = Win32Interop.VK_V;
        inputs[2].U.ki.dwFlags = KeyboardFlags.KEYEVENTF_KEYUP;

        // 4. Ctrl Up
        inputs[3].type = InputTypes.INPUT_KEYBOARD;
        inputs[3].U.ki.wVk = Win32Interop.VK_CONTROL;
        inputs[3].U.ki.dwFlags = KeyboardFlags.KEYEVENTF_KEYUP;

        Win32Interop.SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }
}
