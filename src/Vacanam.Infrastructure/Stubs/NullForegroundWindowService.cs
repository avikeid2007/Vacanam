using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Infrastructure.Stubs;

/// <summary>
/// [MOCK] Null implementation of IForegroundWindowService.
/// Used in Phase 1. Will be replaced by Win32 GetForegroundWindow implementation in Phase 2.
/// </summary>
internal sealed class NullForegroundWindowService : IForegroundWindowService
{
    public ApplicationContext GetCurrentContext() => ApplicationContext.Unknown;
}
