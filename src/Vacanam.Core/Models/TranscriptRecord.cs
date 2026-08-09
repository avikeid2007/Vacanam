namespace Vacanam.Core.Models;

/// <summary>
/// Domain model representing a saved dictation transcript history entry.
/// </summary>
public sealed record TranscriptRecord(
    long Id,
    DateTime TimestampUtc,
    string RawTranscript,
    string FinalText,
    string TargetApp,
    double DurationSeconds,
    bool WasAiEnhanced
);
