namespace Vacanam.Core.Interfaces;

/// <summary>
/// Audio cues played during speech-to-text pipeline lifecycle events.
/// </summary>
public enum AudioCue
{
    /// <summary>Played when the hotkey is pressed and audio capture starts.</summary>
    Start,

    /// <summary>Played when the hotkey is released and processing begins.</summary>
    Stop,

    /// <summary>Played when transcript processing and text injection complete successfully.</summary>
    Success,

    /// <summary>Played when recording is cancelled, too short, or an error occurs.</summary>
    Error
}

/// <summary>
/// Service responsible for playing low-latency earcons and auditory feedback.
/// </summary>
public interface IAudioFeedbackService
{
    /// <summary>
    /// Plays the specified audio cue asynchronously if audio feedback is enabled in settings.
    /// </summary>
    /// <param name="cue">The audio cue to play.</param>
    void Play(AudioCue cue);
}
