namespace Vacanam.Core.Interfaces;

/// <summary>
/// Transcribes audio data to text using a local speech recognition model.
/// All implementations must perform inference off the UI thread.
/// </summary>
public interface ISpeechRecognizer : IDisposable
{
    /// <summary>True if a model is loaded and ready for transcription.</summary>
    bool IsReady { get; }

    /// <summary>Raised when a partial transcript segment is available (streaming).</summary>
    event EventHandler<TranscriptSegmentEventArgs> SegmentReceived;

    /// <summary>
    /// Transcribes the provided PCM audio stream to text.
    /// Returns the complete transcript when done.
    /// </summary>
    Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken = default);

    /// <summary>Loads the configured model into memory. Idempotent if already loaded.</summary>
    Task LoadModelAsync(CancellationToken cancellationToken = default);

    /// <summary>Unloads the model and frees GPU/CPU memory.</summary>
    Task UnloadModelAsync();
}

public sealed class TranscriptSegmentEventArgs(string text, bool isFinal) : EventArgs
{
    public string Text { get; } = text;
    public bool IsFinal { get; } = isFinal;
}
