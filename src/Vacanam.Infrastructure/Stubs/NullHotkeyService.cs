#pragma warning disable CS0067 // [MOCK] Event declared but not raised in stub implementation
using Vacanam.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vacanam.Infrastructure.Stubs;

/// <summary>
/// [MOCK] Null implementation of IGlobalHotkeyService.
/// Used in Phase 1 (App Shell). Will be replaced by Win32 RegisterHotKey in Phase 2.
/// </summary>
internal sealed class NullHotkeyService(ILogger<NullHotkeyService> logger) : IGlobalHotkeyService
{
    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public bool IsRegistered => false;

    public bool Register(nint windowHandle)
    {
        logger.LogWarning("[MOCK] NullHotkeyService.Register — hotkey registration not yet implemented (Phase 2).");
        return false;
    }

    public void Unregister() { }

    public bool UpdateRegistration(nint windowHandle, int modifiers, int virtualKey)
    {
        logger.LogWarning("[MOCK] NullHotkeyService.UpdateRegistration — not yet implemented.");
        return false;
    }

    public void Dispose() { }
}

