using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Input.Strategies;

/// <summary>
/// High-level text injector that orchestrates the 3-strategy cascade:
/// 1. Clipboard + Ctrl+V (Primary)
/// 2. SendInput Unicode (Fallback 1 / Preferred for terminals)
/// 3. UI Automation ValuePattern (Fallback 2)
/// </summary>
public sealed class CompositeTextInjector : ITextInjector
{
    private readonly ClipboardTextInjector _clipboardInjector;
    private readonly SendInputTextInjector _sendInputInjector;
    private readonly UiAutomationTextInjector _uiAutomationInjector;
    private readonly ILogger<CompositeTextInjector> _logger;

    private static readonly HashSet<string> TerminalProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd", "powershell", "pwsh", "wt", "conhost", "bash", "wsl"
    };

    public CompositeTextInjector(
        ClipboardTextInjector clipboardInjector,
        SendInputTextInjector sendInputInjector,
        UiAutomationTextInjector uiAutomationInjector,
        ILogger<CompositeTextInjector> logger)
    {
        _clipboardInjector = clipboardInjector;
        _sendInputInjector = sendInputInjector;
        _uiAutomationInjector = uiAutomationInjector;
        _logger = logger;
    }

    public async Task InjectAsync(string text, ApplicationContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Terminal processes prefer SendInput directly to avoid modifying terminal selection/clipboard
        if (!string.IsNullOrEmpty(context.ProcessName) && TerminalProcessNames.Contains(context.ProcessName))
        {
            _logger.LogInformation("Terminal app detected ({Process}). Using SendInput strategy.", context.ProcessName);
            await _sendInputInjector.InjectAsync(text, context, cancellationToken);
            return;
        }

        // Standard Cascade: Clipboard -> SendInput -> UIAutomation
        try
        {
            await _clipboardInjector.InjectAsync(text, context, cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary Clipboard strategy failed. Cascading to SendInput strategy.");
        }

        try
        {
            await _sendInputInjector.InjectAsync(text, context, cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback SendInput strategy failed. Cascading to UI Automation strategy.");
        }

        try
        {
            await _uiAutomationInjector.InjectAsync(text, context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All text injection strategies failed for HWND={Hwnd:X}", context.WindowHandle);
        }
    }
}
