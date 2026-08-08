using Vacanam.Core.Models;

namespace Vacanam.Core.Interfaces;

/// <summary>
/// Detects and executes voice commands from a transcript.
/// Uses a strict whitelist — no arbitrary command execution is permitted.
/// </summary>
public interface IVoiceCommandProcessor
{
    /// <summary>
    /// Analyses the transcript for a voice command.
    /// Returns a result describing whether a command was found and what action to take.
    /// </summary>
    Task<VoiceCommandResult> ProcessAsync(
        string transcript,
        ApplicationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of voice command detection and execution.</summary>
public sealed record VoiceCommandResult(
    bool WasCommand,
    string? CommandName,
    string? ProcessedText,
    string? ErrorMessage = null)
{
    /// <summary>No command was detected; treat transcript as plain dictation.</summary>
    public static readonly VoiceCommandResult NotACommand = new(false, null, null);
}
