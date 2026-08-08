using Vacanam.Core.Enums;

namespace Vacanam.Core.Models;

/// <summary>
/// Immutable data bag describing a single recording session from start to finish.
/// Created when recording begins and enriched as the pipeline executes.
/// </summary>
public sealed record RecordingSession
{
    /// <summary>Unique identifier for this session (used in history).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp when recording started.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp when recording stopped. Null while still recording.</summary>
    public DateTimeOffset? StoppedAt { get; init; }

    /// <summary>Duration of the audio captured.</summary>
    public TimeSpan? AudioDuration => StoppedAt.HasValue ? StoppedAt.Value - StartedAt : null;

    /// <summary>The application context captured at the start of recording.</summary>
    public ApplicationContext Context { get; init; } = ApplicationContext.Unknown;

    /// <summary>Processing mode selected for this session.</summary>
    public ProcessingMode Mode { get; init; } = ProcessingMode.Fast;

    /// <summary>Raw transcript from Whisper (null until transcription completes).</summary>
    public string? RawTranscript { get; init; }

    /// <summary>Final text after optional LLM processing (null until processing completes).</summary>
    public string? FinalText { get; init; }

    /// <summary>True if the session completed successfully and text was injected.</summary>
    public bool WasSuccessful { get; init; }

    /// <summary>Error message if the session failed. Null on success.</summary>
    public string? ErrorMessage { get; init; }
}
