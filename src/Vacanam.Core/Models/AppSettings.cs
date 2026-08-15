using Vacanam.Core.Enums;

namespace Vacanam.Core.Models;

public sealed class AppSettings
{
    public const string SectionName = "Vacanam";

    public GeneralSettings General { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public SpeechSettings Speech { get; set; } = new();
    public AiSettings Ai { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
    public VoiceCommandsSettings VoiceCommands { get; set; } = new();
}

public sealed class GeneralSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool ShowTrayNotifications { get; set; } = true;
    public ProcessingMode DefaultMode { get; set; } = ProcessingMode.Fast;
}

public sealed class HotkeySettings
{
    public int Modifiers { get; set; } = 2;
    public int VirtualKey { get; set; } = 0x20;
    public bool PushToTalk { get; set; } = true;
}

public sealed class AudioSettings
{
    public string PreferredDeviceId { get; set; } = string.Empty;
    public int SampleRate { get; set; } = 16000;
    public bool EnableVad { get; set; } = true;
    public double VadThreshold { get; set; } = 0.02;
}

public sealed class SpeechSettings
{
    public string ModelSize { get; set; } = "small";
    public string Device { get; set; } = "Auto";
    public string Language { get; set; } = "en";
}

public sealed class AiSettings
{
    public bool Enabled { get; set; } = false;
    public string ModelFile { get; set; } = "Qwen2.5-0.5B-Instruct-Q4_K_M.gguf";
    public int GpuLayers { get; set; } = -1;
    public int MaxTokens { get; set; } = 512;
    public bool ConservativeMode { get; set; } = true;

    /// <summary>System prompt for LLM text refinement &amp; grammar correction.</summary>
    public string SystemPrompt { get; set; } =
        "You are a silent, ultra-fast text polish engine. Your job is to clean up transcribed speech.\n" +
        "RULES:\n" +
        "1. Fix capitalization, punctuation, and obvious grammar errors.\n" +
        "2. Remove filler words (uh, um, like, you know).\n" +
        "3. DO NOT change facts, numbers, names, code, or intentional word choices.\n" +
        "4. Return ONLY the cleaned text. DO NOT add notes, explanations, or quotes around the output.";
}

public sealed class PrivacySettings
{
    public bool SaveHistory { get; set; } = false;
    public int MaxHistoryEntries { get; set; } = 1000;
}

public sealed class VoiceCommandsSettings
{
    public bool Enabled { get; set; } = true;
    public bool EnableSmartPunctuation { get; set; } = true;
    public List<CustomSnippet> CustomSnippets { get; set; } =
    [
        new("insert signature", "Best regards,\n[Your Name]"),
        new("insert date", "{DATE}"),
        new("insert time", "{TIME}")
    ];
}

public sealed class CustomSnippet
{
    public string TriggerPhrase { get; set; } = string.Empty;
    public string ExpansionText { get; set; } = string.Empty;

    public CustomSnippet() { }

    public CustomSnippet(string triggerPhrase, string expansionText)
    {
        TriggerPhrase = triggerPhrase;
        ExpansionText = expansionText;
    }
}

