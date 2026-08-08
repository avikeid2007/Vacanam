namespace Vacanam.Core.Enums;

/// <summary>
/// Represents the current operational state of the Vacanam application.
/// The state machine drives overlay UI, hotkey handling, and processing pipeline behaviour.
/// </summary>
public enum VacanamState
{
    /// <summary>App is running in the tray, waiting for the hotkey.</summary>
    Idle,

    /// <summary>Hotkey received; audio pipeline is initialising.</summary>
    StartingRecording,

    /// <summary>Microphone is actively capturing audio.</summary>
    Recording,

    /// <summary>Hotkey released or toggled off; finalising audio buffer.</summary>
    StoppingRecording,

    /// <summary>Audio is being transcribed by the local Whisper model.</summary>
    Transcribing,

    /// <summary>Transcript is being processed by the local LLM (AI mode only).</summary>
    Processing,

    /// <summary>Final text is being injected into the target application.</summary>
    Inserting,

    /// <summary>Text has been successfully inserted. Transitioning back to Idle.</summary>
    Completed,

    /// <summary>An error occurred. Details available via IVacanamStateService.</summary>
    Error
}
