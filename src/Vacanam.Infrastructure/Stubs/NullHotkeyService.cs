#pragma warning disable CS0067 // [MOCK] Event declared but not raised in stub implementation
using Vacanam.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vacanam.Infrastructure.Stubs;

/// <summary>
/// [MOCK] Null implementation of IGlobalHotkeyService.
/// </summary>
internal sealed class NullHotkeyService(ILogger<NullHotkeyService> logger) : IGlobalHotkeyService
{
    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler? AiTransformHotkeyPressed;
    public event EventHandler? AiTransformHotkeyReleased;

    public bool IsRegistered => false;
    public bool IsAiTransformRegistered => false;

    public bool Register(nint windowHandle)
    {
        logger.LogWarning("[MOCK] NullHotkeyService.Register - hotkey registration not implemented in stub.");
        return false;
    }

    public void Unregister() { }

    public bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey)
    {
        logger.LogWarning("[MOCK] NullHotkeyService.UpdateRegistration - not implemented in stub.");
        return false;
    }

    public bool UpdateAiTransformRegistration(nint windowHandle, int modifiers, int virtualKey)
    {
        logger.LogWarning("[MOCK] NullHotkeyService.UpdateAiTransformRegistration - not implemented in stub.");
        return false;
    }

    public void SuppressHoldDetection(bool suppress) { }

    public void Dispose() { }
}
