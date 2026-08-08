namespace Vacanam.Core.Interfaces;

/// <summary>
/// Processes a raw transcript with a local LLM to improve grammar, clarity,
/// and formatting. Conservative: never changes meaning.
/// </summary>
public interface ITextProcessor : IDisposable
{
    /// <summary>True if an LLM model is loaded and AI mode is active.</summary>
    bool IsEnabled { get; }

    /// <summary>Raised as the LLM streams individual tokens.</summary>
    event EventHandler<TokenEventArgs> TokenGenerated;

    /// <summary>
    /// Processes the raw transcript using the local LLM and returns the final text.
    /// </summary>
    Task<string> ProcessAsync(
        string rawTranscript,
        Models.ApplicationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the LLM model. Idempotent if already loaded.</summary>
    Task LoadModelAsync(CancellationToken cancellationToken = default);

    /// <summary>Unloads the LLM and frees GPU/CPU memory.</summary>
    Task UnloadModelAsync();
}

public sealed class TokenEventArgs(string token) : EventArgs
{
    public string Token { get; } = token;
}
