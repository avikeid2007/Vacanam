namespace Vacanam.Core.Interfaces;

/// <summary>
/// Detects whether a buffer of audio samples contains human speech.
/// Used to trim silence from recordings before passing to Whisper.
/// </summary>
public interface IVoiceActivityDetector
{
    /// <summary>
    /// Returns true if the given audio buffer is likely to contain speech.
    /// </summary>
    /// <param name="audio">32-bit float samples, normalised to [-1.0, 1.0].</param>
    bool IsSpeech(ReadOnlySpan<float> audio);
}
