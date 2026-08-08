using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Windows.Interop;

namespace Vacanam.Input.Strategies;

/// <summary>
/// Fallback text injection strategy using Win32 SendInput (Unicode keystrokes).
/// Ideal for terminals (cmd.exe, Windows Terminal) or applications that restrict clipboard paste.
/// Does not touch or modify the clipboard.
/// </summary>
public sealed class SendInputTextInjector : ITextInjector
{
    private readonly ILogger<SendInputTextInjector> _logger;

    public SendInputTextInjector(ILogger<SendInputTextInjector> logger)
    {
        _logger = logger;
    }

    public async Task InjectAsync(string text, ApplicationContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return;

        _logger.LogInformation("Injecting {Len} chars via SendInput (Unicode keystrokes) to HWND={Hwnd:X} ({Process})",
            text.Length, context.WindowHandle, context.ProcessName);

        if (context.WindowHandle != IntPtr.Zero)
        {
            Win32Interop.SetForegroundWindow(context.WindowHandle);
            await Task.Delay(30, cancellationToken);
        }

        // Build 2 INPUT entries per character (key down, key up)
        var inputs = new INPUT[text.Length * 2];

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            // Key Down
            inputs[i * 2].type = InputTypes.INPUT_KEYBOARD;
            inputs[i * 2].U.ki.wScan = ch;
            inputs[i * 2].U.ki.dwFlags = KeyboardFlags.KEYEVENTF_UNICODE;

            // Key Up
            inputs[i * 2 + 1].type = InputTypes.INPUT_KEYBOARD;
            inputs[i * 2 + 1].U.ki.wScan = ch;
            inputs[i * 2 + 1].U.ki.dwFlags = KeyboardFlags.KEYEVENTF_UNICODE | KeyboardFlags.KEYEVENTF_KEYUP;
        }

        // Send in batches if text is very long
        const int BatchSize = 100; // 50 chars per batch
        for (int offset = 0; offset < inputs.Length; offset += BatchSize)
        {
            if (cancellationToken.IsCancellationRequested) break;

            int count = Math.Min(BatchSize, inputs.Length - offset);
            var batch = new INPUT[count];
            Array.Copy(inputs, offset, batch, 0, count);

            Win32Interop.SendInput((uint)count, batch, INPUT.Size);
            await Task.Delay(5, cancellationToken);
        }
    }
}
