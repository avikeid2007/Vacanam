using Vacanam.Core.Enums;

namespace Vacanam.Core.Models;

/// <summary>
/// Strongly-typed application settings model. Bound from appsettings.json via IOptions&lt;AppSettings&gt;.
/// </summary>
public sealed class AppSettings
{
    public const string SectionName = "Vacanam";

    // -- General --------------------------------------------------------------
    public GeneralSettings General { get; set; } = new();

    // -- Hotkeys --------------------------------------------------------------
    public HotkeySettings Hotkeys { get; set; } = new();

    // -- Audio -----------------------------------------------------------------
    public AudioSettings Audio { get; set; } = new();

    // -- Speech (Whisper) -----------------------------------------------------
    public SpeechSettings Speech { get; set; } = new();

    // -- AI (LLM) -------------------------------------------------------------
    public AiSettings Ai { get; set; } = new();

    // -- Privacy ---------------------------------------------------------------
    public PrivacySettings Privacy { get; set; } = new();
}

public sealed class GeneralSettings
{
    /// <summary>Start Vacanam automatically when Windows starts.</summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>Show a balloon notification when recording starts.</summary>
    public bool ShowTrayNotifications { get; set; } = true;

    /// <summary>Default processing mode.</summary>
    public ProcessingMode DefaultMode { get; set; } = ProcessingMode.Fast;
}

public sealed class HotkeySettings
{
    /// <summary>Modifier keys as bitmask (1=Alt, 2=Ctrl, 4=Shift, 8=Win).</summary>
    public int Modifiers { get; set; } = 2; // Ctrl

    /// <summary>Virtual key code for the hotkey (0x20 = Space).</summary>
    public int VirtualKey { get; set; } = 0x20;

    /// <summary>True = hold to record; False = toggle on/off.</summary>
    public bool PushToTalk { get; set; } = true;
}

public sealed class AudioSettings
{
    /// <summary>Preferred microphone device ID. Empty = system default.</summary>
    public string PreferredDeviceId { get; set; } = string.Empty;

    /// <summary>Target sample rate in Hz. Whisper requires 16000.</summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>Enable voice activity detection to trim silence.</summary>
    public bool EnableVad { get; set; } = true;

    /// <summary>Silence threshold for VAD (0.0–1.0).</summary>
    public double VadThreshold { get; set; } = 0.02;
}

public sealed class SpeechSettings
{
    /// <summary>Whisper model size to use. Options: tiny, small, medium, large-v3.</summary>
    public string ModelSize { get; set; } = "small";

    /// <summary>Inference device. Options: Auto, CPU, CUDA.</summary>
    public string Device { get; set; } = "Auto";

    /// <summary>Transcription language hint. "auto" for automatic detection.</summary>
    public string Language { get; set; } = "auto";
}

public sealed class AiSettings
{
    /// <summary>Whether LLM processing is enabled. Disabled by default.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Active LLM model filename (relative to Models/LLM directory).
    /// Options: phi-3.5-mini-instruct-Q4_K_M.gguf, llama-3.2-3b-Q4_K_M.gguf, gemma-2-2b-it-Q4_K_M.gguf
    /// </summary>
    public string ModelFile { get; set; } = string.Empty;

    /// <summary>GPU layers to offload. -1 = auto-detect based on VRAM.</summary>
    public int GpuLayers { get; set; } = -1;

    /// <summary>Maximum tokens for LLM response.</summary>
    public int MaxTokens { get; set; } = 512;

    /// <summary>Conservative mode: never change meaning, only fix grammar.</summary>
    public bool ConservativeMode { get; set; } = true;
}

public sealed class PrivacySettings
{
    /// <summary>
    /// Save transcript history to local SQLite database.
    /// OPT-IN: disabled by default.
    /// </summary>
    public bool SaveHistory { get; set; } = false;

    /// <summary>Maximum number of history entries to retain.</summary>
    public int MaxHistoryEntries { get; set; } = 1000;
}
