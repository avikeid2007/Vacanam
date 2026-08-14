namespace Vacanam.Core.Models;

/// <summary>
/// Represents a language supported by OpenAI Whisper speech recognition.
/// </summary>
public sealed record WhisperLanguage(string Code, string Name, string NativeName = "")
{
    /// <summary>
    /// Friendly display text for UI dropdowns (e.g. "English (en)" or "Hindi — हिन्दी (hi)").
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(NativeName) || string.Equals(NativeName, Name, StringComparison.OrdinalIgnoreCase)
        ? $"{Name} ({Code})"
        : $"{Name} — {NativeName} ({Code})";

    public override string ToString() => DisplayName;
}
