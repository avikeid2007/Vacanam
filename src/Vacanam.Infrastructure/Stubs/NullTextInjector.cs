using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Microsoft.Extensions.Logging;

namespace Vacanam.Infrastructure.Stubs;

/// <summary>
/// [MOCK] Null implementation of ITextInjector.
/// Used in Phase 1 (App Shell). Will be replaced by clipboard/SendInput/UIA in Phase 5.
/// </summary>
internal sealed class NullTextInjector(ILogger<NullTextInjector> logger) : ITextInjector
{
    public Task InjectAsync(string text, ApplicationContext context, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("[MOCK] NullTextInjector.InjectAsync — text injection not yet implemented (Phase 5). Text: {Length} chars", text.Length);
        return Task.CompletedTask;
    }
}
