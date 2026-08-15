using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Windows.Interop;

namespace Vacanam.Speech.Commands;

/// <summary>
/// Detects and executes voice action commands (e.g. "select all", "undo that", "press enter")
/// and expands custom voice snippets/macros.
/// </summary>
public sealed class VoiceCommandProcessor : IVoiceCommandProcessor
{
    private readonly IOptions<AppSettings> _settings;
    private readonly ILogger<VoiceCommandProcessor> _logger;

    public VoiceCommandProcessor(
        IOptions<AppSettings> settings,
        ILogger<VoiceCommandProcessor> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<VoiceCommandResult> ProcessAsync(
        string transcript,
        ApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript) || !_settings.Value.VoiceCommands.Enabled)
        {
            return VoiceCommandResult.NotACommand;
        }

        // Clean transcript for command matching (strip punctuation & normalize whitespace)
        string cleaned = CleanCommandText(transcript);

        _logger.LogDebug("Evaluating potential voice command: '{Cleaned}' (raw: '{Raw}')", cleaned, transcript);

        // ── 1. Built-in Action Commands ──────────────────────────────────────
        switch (cleaned)
        {
            case "select all":
                _logger.LogInformation("Executing Voice Command: Select All (Ctrl+A)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_A, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Select All", null);

            case "copy that":
            case "copy":
                _logger.LogInformation("Executing Voice Command: Copy (Ctrl+C)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_C, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Copy", null);

            case "paste that":
            case "paste":
                _logger.LogInformation("Executing Voice Command: Paste (Ctrl+V)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_V, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Paste", null);

            case "undo that":
            case "undo":
                _logger.LogInformation("Executing Voice Command: Undo (Ctrl+Z)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_Z, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Undo", null);

            case "redo that":
            case "redo":
                _logger.LogInformation("Executing Voice Command: Redo (Ctrl+Y)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_Y, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Redo", null);

            case "save document":
            case "save file":
            case "save":
                _logger.LogInformation("Executing Voice Command: Save (Ctrl+S)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_S, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Save Document", null);

            case "press enter":
            case "hit enter":
            case "enter key":
                _logger.LogInformation("Executing Voice Command: Press Enter");
                await KeySimulator.SendKeyAsync(Win32Interop.VK_RETURN, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Press Enter", null);

            case "press tab":
            case "hit tab":
            case "tab key":
                _logger.LogInformation("Executing Voice Command: Press Tab");
                await KeySimulator.SendKeyAsync(Win32Interop.VK_TAB, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Press Tab", null);

            case "press escape":
            case "hit escape":
            case "escape key":
                _logger.LogInformation("Executing Voice Command: Press Escape");
                await KeySimulator.SendKeyAsync(Win32Interop.VK_ESCAPE, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Press Escape", null);

            case "delete line":
                _logger.LogInformation("Executing Voice Command: Delete Line");
                await KeySimulator.DeleteLineAsync(context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Delete Line", null);

            case "delete word":
            case "backspace":
                _logger.LogInformation("Executing Voice Command: Delete Word / Backspace");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_CONTROL, Win32Interop.VK_BACK, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Delete Word", null);

            case "switch window":
            case "next window":
                _logger.LogInformation("Executing Voice Command: Switch Window (Alt+Tab)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_MENU, Win32Interop.VK_TAB, IntPtr.Zero, cancellationToken);
                return new VoiceCommandResult(true, "Switch Window", null);

            case "close window":
                _logger.LogInformation("Executing Voice Command: Close Window (Alt+F4)");
                await KeySimulator.SendModifiedKeyAsync(Win32Interop.VK_MENU, Win32Interop.VK_F4, context.WindowHandle, cancellationToken);
                return new VoiceCommandResult(true, "Close Window", null);

            case "lock computer":
            case "lock screen":
                _logger.LogInformation("Executing Voice Command: Lock Workstation");
                Win32Interop.LockWorkStation();
                return new VoiceCommandResult(true, "Lock Screen", null);
        }

        // ── 2. Custom Snippets / Expansion Macros ─────────────────────────────
        var snippets = _settings.Value.VoiceCommands.CustomSnippets;
        if (snippets is { Count: > 0 })
        {
            foreach (var snippet in snippets)
            {
                if (string.IsNullOrWhiteSpace(snippet.TriggerPhrase)) continue;

                string cleanedTrigger = CleanCommandText(snippet.TriggerPhrase);

                if (string.Equals(cleaned, cleanedTrigger, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cleaned, "insert " + cleanedTrigger, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cleaned, "snippet " + cleanedTrigger, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Expanding Custom Voice Snippet: '{Trigger}'", snippet.TriggerPhrase);
                    string expanded = ExpandSnippetMacros(snippet.ExpansionText);
                    return new VoiceCommandResult(true, $"Snippet: {snippet.TriggerPhrase}", expanded);
                }
            }
        }

        return VoiceCommandResult.NotACommand;
    }

    private static string CleanCommandText(string text)
    {
        // Strip punctuation and normalize extra spaces to single space
        string stripped = Regex.Replace(text, @"[^\w\s]", "").Trim();
        return Regex.Replace(stripped, @"\s+", " ").ToLowerInvariant();
    }

    private static string ExpandSnippetMacros(string template)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        return template
            .Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{TIME}", DateTime.Now.ToString("HH:mm"), StringComparison.OrdinalIgnoreCase)
            .Replace("{DATETIME}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"), StringComparison.OrdinalIgnoreCase);
    }
}
