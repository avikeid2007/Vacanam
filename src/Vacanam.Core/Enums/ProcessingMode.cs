namespace Vacanam.Core.Enums;

/// <summary>
/// Controls how the transcribed text is processed before being injected.
/// </summary>
public enum ProcessingMode
{
    /// <summary>
    /// Fast mode: Speech -> Whisper -> minimal cleanup -> inject.
    /// No LLM involved. Lowest latency.
    /// </summary>
    Fast,

    /// <summary>
    /// AI mode: Speech -> Whisper -> LLM grammar/clarity correction -> inject.
    /// Requires a local LLM model to be loaded.
    /// </summary>
    AI,

    /// <summary>
    /// Command mode: Speech -> Whisper -> command detection -> action execution.
    /// Used for editing selected text or triggering predefined workflows.
    /// </summary>
    Command
}
