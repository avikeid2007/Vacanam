#pragma warning disable CS0067 // [MOCK] Event declared but not raised in stub implementation
using Vacanam.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vacanam.Infrastructure.Stubs;

/// <summary>
/// [MOCK] Null implementation of ISpeechRecognizer.
/// Used in Phase 1 (App Shell). Will be replaced by Whisper.net implementation in Phase 4.
/// </summary>
internal sealed class NullSpeechRecognizer(ILogger<NullSpeechRecognizer> logger) : ISpeechRecognizer
{
    public bool IsReady => false;
    public event EventHandler<TranscriptSegmentEventArgs>? SegmentReceived;

    public Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("[MOCK] NullSpeechRecognizer.TranscribeAsync — no transcription until Phase 4.");
        return Task.FromResult("[MOCK] Transcription not yet implemented.");
    }

    public Task LoadModelAsync(CancellationToken cancellationToken = default)
    {
        logger.LogWarning("[MOCK] NullSpeechRecognizer.LoadModelAsync — Whisper not yet integrated.");
        return Task.CompletedTask;
    }

    public Task UnloadModelAsync() => Task.CompletedTask;
    public void Dispose() { }
}

