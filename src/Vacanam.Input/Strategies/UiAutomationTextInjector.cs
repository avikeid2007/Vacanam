using System.Windows.Automation;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Input.Strategies;

/// <summary>
/// Fallback text injection strategy using Windows UI Automation.
/// Locates focused text element via UI Automation tree and sets ValuePattern.
/// </summary>
public sealed class UiAutomationTextInjector : ITextInjector
{
    private readonly ILogger<UiAutomationTextInjector> _logger;

    public UiAutomationTextInjector(ILogger<UiAutomationTextInjector> logger)
    {
        _logger = logger;
    }

    public Task InjectAsync(string text, ApplicationContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text)) return Task.CompletedTask;

        _logger.LogInformation("Attempting UI Automation injection to HWND={Hwnd:X}", context.WindowHandle);

        try
        {
            AutomationElement? focusedElement = AutomationElement.FocusedElement;
            if (focusedElement is null)
            {
                _logger.LogWarning("UI Automation: No focused element found.");
                return Task.CompletedTask;
            }

            if (focusedElement.TryGetCurrentPattern(ValuePattern.Pattern, out object? patternObj) && patternObj is ValuePattern valuePattern)
            {
                if (!valuePattern.Current.IsReadOnly)
                {
                    string existing = valuePattern.Current.Value ?? string.Empty;
                    valuePattern.SetValue(existing + text);
                    _logger.LogInformation("Successfully injected text via UI Automation ValuePattern.");
                    return Task.CompletedTask;
                }
            }

            _logger.LogWarning("Focused element does not support editable ValuePattern.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UI Automation injection failed.");
        }

        return Task.CompletedTask;
    }
}
