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
    public int Modifiers { get; set; } = 2; // Ctrl
    public int VirtualKey { get; set; } = 0x20; // Space
    public bool PushToTalk { get; set; } = true;

    /// <summary>Whether the secondary 'Ask AI' / Voice Transform hotkey is active.</summary>
    public bool EnableAiTransformHotkey { get; set; } = true;

    /// <summary>Modifiers bitmask for 'Ask AI' hotkey (4 = Shift).</summary>
    public int AiTransformModifiers { get; set; } = 4;

    /// <summary>Virtual key code for 'Ask AI' hotkey (0x20 = Space).</summary>
    public int AiTransformVirtualKey { get; set; } = 0x20;
}

public sealed class AudioSettings
{
    public string PreferredDeviceId { get; set; } = string.Empty;
    public int SampleRate { get; set; } = 16000;
    public bool EnableVad { get; set; } = true;
    public double VadThreshold { get; set; } = 0.02;
    public bool EnableSoundEffects { get; set; } = true;
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
    public bool EnableContextProfiles { get; set; } = true;
    public List<AppContextProfile> ContextProfiles { get; set; } = DefaultContextProfiles();

    /// <summary>System prompt for LLM text refinement &amp; grammar correction.</summary>
    public string SystemPrompt { get; set; } =
        "You are a silent, ultra-fast text polish engine. Your job is to clean up transcribed speech.\n" +
        "RULES:\n" +
        "1. Fix capitalization, punctuation, and obvious grammar errors.\n" +
        "2. Remove filler words (uh, um, like, you know).\n" +
        "3. DO NOT change facts, numbers, names, code, or intentional word choices.\n" +
        "4. Return ONLY the cleaned text. DO NOT add notes, explanations, or quotes around the output.";

    /// <summary>System prompt for Voice Transform when text is selected.</summary>
    public string TransformSystemPrompt { get; set; } =
        "You are an expert desktop AI assistant. The user has highlighted reference text in their active application and provided a voice instruction.\n" +
        "YOUR ROLE:\n" +
        "- If the instruction asks to REPLY, RESPOND, or FOLLOW UP (e.g. 'reply to this email', 'respond saying thanks', 'write reply', 'we apply for this mail'):\n" +
        "  Draft a clear, professional, complete reply/response to the reference text.\n" +
        "- If the instruction asks to REWRITE, EDIT, PARAPHRASE, TRANSLATE, or POLISH (e.g. 'make this professional', 'fix grammar', 'translate to Spanish'):\n" +
        "  Rewrite the reference text following the user's instructions.\n" +
        "- If the instruction asks to SUMMARIZE, EXPLAIN, or EXTRACT:\n" +
        "  Provide the requested summary, explanation, or extracted details based on the reference text.\n" +
        "CRITICAL RULES:\n" +
        "1. Output ONLY the resulting content to be inserted.\n" +
        "2. NEVER simply repeat or echo the reference text unchanged. Always execute the requested reply, rewrite, or action.\n" +
        "3. Do NOT add conversational filler or preamble (NO 'Here is your reply:', 'Sure!', 'Transformed text:').\n" +
        "4. Do NOT add notes, explanations, or quotes around the output.";

    /// <summary>System prompt for Ask AI when no text is selected (direct generation).</summary>
    public string AskAiSystemPrompt { get; set; } =
        "You are a direct, concise voice AI assistant.\n" +
        "Answer the user's prompt directly and accurately.\n" +
        "RULES:\n" +
        "1. Output ONLY the direct answer/content requested for immediate insertion into the active application.\n" +
        "2. Do NOT include conversational greetings ('Sure!', 'Here you go:') or trailing commentary.\n" +
        "3. Format cleanly (e.g. code blocks, bullet points) as appropriate.";

    public static List<AppContextProfile> DefaultContextProfiles() =>
    [
        new(
            "coding",
            "Coding & Terminal",
            "Visual Studio, VS Code, JetBrains, Windows Terminal, PowerShell",
            "code, devenv, idea64, rider64, pycharm64, clion64, windowsterminal, powershell, cmd, wt, cursor, sublime_text",
            "TARGET CONTEXT: The user is dictating inside a coding IDE or command-line terminal. Preserve programming terms, function/variable names, camelCase, snake_case, PascalCase, CLI flags, and technical symbols. Format code or shell commands cleanly. Do NOT convert technical abbreviations into prose."
        ),
        new(
            "chat",
            "Chat & Messaging",
            "Slack, Microsoft Teams, Discord, Telegram, WhatsApp",
            "slack, teams, discord, telegram, whatsapp, signal, ms-teams",
            "TARGET CONTEXT: The user is dictating in a chat or messaging application. Use a natural, conversational, punchy tone. Preserve casual phrasing, sentence-casing, and emojis where appropriate. Do not make the message overly stiff or academic."
        ),
        new(
            "email",
            "Email & Documents",
            "Outlook, Microsoft Word, Thunderbird, Google Docs",
            "outlook, olk, winword, thunderbird, excel, powerpnt",
            "TARGET CONTEXT: The user is dictating a professional email or formal document. Use a polite, professional business tone. Organize into clear paragraphs with proper email greeting and sign-off capitalization where appropriate."
        ),
        new(
            "notes",
            "Notes & Markdown",
            "Notion, Obsidian, OneNote, Logseq",
            "notion, obsidian, onenote, logseq",
            "TARGET CONTEXT: The user is taking notes or drafting in markdown. Format lists as clean bullet points (-) or numbered steps where applicable, and maintain structured, concise organization."
        )
    ];
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

public sealed class AppContextProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProcessMatches { get; set; } = string.Empty;
    public string PromptInstruction { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public AppContextProfile() { }

    public AppContextProfile(string id, string name, string description, string processMatches, string promptInstruction, bool isEnabled = true)
    {
        Id = id;
        Name = name;
        Description = description;
        ProcessMatches = processMatches;
        PromptInstruction = promptInstruction;
        IsEnabled = isEnabled;
    }
}

