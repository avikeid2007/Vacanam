using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vacanam.Core.Constants;
using Vacanam.Core.Enums;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Infrastructure.Configuration;

namespace Vacanam.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window. Manages all settings categories,
/// model download statuses, live model selection, and local SQLite history search.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsManager _settingsManager;
    private readonly IOptions<AppSettings> _options;
    private readonly IModelManager _modelManager;
    private readonly IAudioRecorder _audioRecorder;
    private readonly Vacanam.LLM.Model.LlmModelManager _llmModelManager;
    private readonly ITranscriptHistoryRepository _historyRepository;
    private readonly IAutoStartService _autoStartService;
    private readonly ILogger<SettingsViewModel> _logger;
    private AppSettings _originalSettings = new();

    public SettingsViewModel(
        SettingsManager settingsManager,
        IOptions<AppSettings> options,
        IModelManager modelManager,
        IAudioRecorder audioRecorder,
        Vacanam.LLM.Model.LlmModelManager llmModelManager,
        ITranscriptHistoryRepository historyRepository,
        IAutoStartService autoStartService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsManager = settingsManager;
        _options = options;
        _modelManager = modelManager;
        _audioRecorder = audioRecorder;
        _llmModelManager = llmModelManager;
        _historyRepository = historyRepository;
        _autoStartService = autoStartService;
        _logger = logger;

        LoadFromSettings(_settingsManager.Load());
        RefreshModelStatuses();
        _ = RefreshHistoryAsync();
    }

    // ── General Tab ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _startWithWindows = false;

    [ObservableProperty]
    private bool _showTrayNotifications = true;

    [ObservableProperty]
    private ProcessingMode _defaultMode = ProcessingMode.Fast;

    public IReadOnlyList<ProcessingMode> AvailableModes { get; } =
        [ProcessingMode.Fast, ProcessingMode.AI, ProcessingMode.Command];

    // ── Hotkeys Tab ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _pushToTalk = true;

    [ObservableProperty]
    private string _hotkeyDisplayText = "Ctrl + Space";

    // ── Audio Tab ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _enableVad = true;

    [ObservableProperty]
    private double _vadThreshold = 0.02;

    [ObservableProperty]
    private string _selectedDeviceId = string.Empty;

    [ObservableProperty]
    private double _micVolume = 100;

    partial void OnMicVolumeChanged(double value)
    {
        if (_audioRecorder is not null)
        {
            _audioRecorder.MasterVolume = (float)(value / 100.0);
        }
        HasChanges = true;
    }

    [ObservableProperty]
    private bool _isMicMuted = false;

    partial void OnIsMicMutedChanged(bool value)
    {
        if (_audioRecorder is not null)
        {
            _audioRecorder.IsMuted = value;
        }
        HasChanges = true;
    }

    // ── Speech Tab ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _whisperModelSize = "small";

    partial void OnWhisperModelSizeChanged(string value)
    {
        RefreshModelStatuses();
        HasChanges = true;
    }

    public List<WhisperModelItem> WhisperModels { get; } =
    [
        new("tiny",      "Ultra Fast",            "~75 MB",   "Fastest response — ideal for quick phrases & low-end PCs"),
        new("small",     "Balanced (Recommended)", "~466 MB",  "Optimal speed & accuracy — recommended for everyday dictation"),
        new("medium",    "High Precision",        "~1.5 GB",  "Higher accuracy — excellent for technical terms & complex speech"),
        new("large-v3",  "Maximum Accuracy",      "~3.1 GB",  "Maximum precision — best accuracy for multi-language & accents"),
    ];

    [ObservableProperty]
    private string _whisperDevice = "Auto";

    public IReadOnlyList<string> WhisperDevices { get; } = ["Auto", "CPU", "CUDA"];

    [ObservableProperty]
    private string _whisperLanguage = "en";

    [ObservableProperty]
    private WhisperLanguage? _selectedLanguage;

    partial void OnWhisperLanguageChanged(string value)
    {
        if (_selectedLanguage?.Code != value)
        {
            _selectedLanguage = AvailableLanguages.FirstOrDefault(l => string.Equals(l.Code, value, StringComparison.OrdinalIgnoreCase)) ?? AvailableLanguages[0];
            OnPropertyChanged(nameof(SelectedLanguage));
        }
        HasChanges = true;
    }

    partial void OnSelectedLanguageChanged(WhisperLanguage? value)
    {
        if (value is not null && _whisperLanguage != value.Code)
        {
            _whisperLanguage = value.Code;
            OnPropertyChanged(nameof(WhisperLanguage));
            HasChanges = true;
        }
    }

    public IReadOnlyList<WhisperLanguage> AvailableLanguages { get; } = WhisperLanguages.All;

    // ── AI Tab ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _aiEnabled = false;

    partial void OnAiEnabledChanged(bool value)
    {
        if (value && !CanEnableAi)
        {
            _aiEnabled = false;
            OnPropertyChanged(nameof(AiEnabled));
            StatusMessage = "Cannot enable AI text enhancement — selected model is not downloaded.";
        }
        else
        {
            HasChanges = true;
        }
    }

    [ObservableProperty]
    private bool _canEnableAi = false;

    [ObservableProperty]
    private string _aiStatusCaption = "⚠️ Download and select an LLM model below to enable AI text enhancement.";

    [ObservableProperty]
    private string _llmModelFile = "Qwen2.5-0.5B-Instruct-Q4_K_M.gguf";

    partial void OnLlmModelFileChanged(string value)
    {
        RefreshModelStatuses();
        HasChanges = true;
    }

    [ObservableProperty]
    private string _systemPrompt = Vacanam.LLM.Prompts.SystemPrompts.DefaultGrammarFix;

    partial void OnSystemPromptChanged(string value)
    {
        HasChanges = true;
    }

    [RelayCommand]
    private void ResetSystemPrompt()
    {
        SystemPrompt = Vacanam.LLM.Prompts.SystemPrompts.DefaultGrammarFix;
        StatusMessage = "System prompt reset to default rules.";
    }

    public List<LlmModelItem> LlmModels { get; } =
    [
        new("Qwen2.5-0.5B-Instruct-Q4_K_M.gguf", "Qwen 2.5 0.5B Instruct", "~398 MB",  "< 600 MB RAM", "Alibaba — Top grammar accuracy for sub-400MB"),
        new("Llama-3.2-1B-Instruct-Q4_K_M.gguf", "Llama 3.2 1B Instruct",  "~808 MB",  "< 1.2 GB RAM", "Meta — High precision grammar refinement"),
    ];

    [ObservableProperty]
    private bool _conservativeMode = true;

    // ── Privacy Tab ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _saveHistory = false;

    [ObservableProperty]
    private int _maxHistoryEntries = 1000;

    // ── History Tab ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _historySearchQuery = string.Empty;

    partial void OnHistorySearchQueryChanged(string value)
    {
        _ = SearchHistoryAsync();
    }

    public ObservableCollection<TranscriptRecord> HistoryRecords { get; } = [];

    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        await SearchHistoryAsync();
    }

    [RelayCommand]
    private async Task SearchHistoryAsync()
    {
        try
        {
            var results = string.IsNullOrWhiteSpace(HistorySearchQuery)
                ? await _historyRepository.GetRecentAsync(100)
                : await _historyRepository.SearchAsync(HistorySearchQuery, 100);

            HistoryRecords.Clear();
            foreach (var record in results)
            {
                HistoryRecords.Add(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search history.");
        }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        try
        {
            await _historyRepository.ClearAllAsync();
            HistoryRecords.Clear();
            StatusMessage = "Transcript history cleared cleanly.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear history.");
        }
    }

    [RelayCommand]
    private async Task DeleteHistoryRecordAsync(long id)
    {
        try
        {
            await _historyRepository.DeleteByIdAsync(id);
            var item = HistoryRecords.FirstOrDefault(r => r.Id == id);
            if (item is not null)
            {
                HistoryRecords.Remove(item);
            }
            StatusMessage = "Transcript record deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete transcript record {Id}.", id);
        }
    }

    [RelayCommand]
    private void CopyHistoryText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            Clipboard.SetText(text);
            StatusMessage = "Copied transcript to clipboard!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard.");
        }
    }

    // ── Status ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasChanges = false;

    // ── Model Commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectWhisperModel(string modelId)
    {
        WhisperModelSize = modelId;
        var item = WhisperModels.FirstOrDefault(m => m.Id == modelId);
        StatusMessage = $"Selected speech engine profile: {item?.DisplayName ?? modelId}";
    }

    [RelayCommand]
    private async Task DownloadWhisperModelAsync(string modelId)
    {
        var item = WhisperModels.FirstOrDefault(m => m.Id == modelId);
        if (item is null) return;

        item.IsDownloading = true;
        item.StatusText = "Downloading...";
        StatusMessage = $"Downloading Whisper {item.DisplayName} model...";

        try
        {
            var progress = new Progress<double>(mb =>
            {
                item.StatusText = $"Downloading ({mb:F1} MB)...";
            });

            await _modelManager.EnsureWhisperModelDownloadedAsync(modelId, progress);

            StatusMessage = $"Whisper {item.DisplayName} model downloaded successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download model {Id}", modelId);
            StatusMessage = $"Download failed for {item.DisplayName}. Check internet connection.";
        }
        finally
        {
            item.IsDownloading = false;
            RefreshModelStatuses();
        }
    }

    [RelayCommand]
    private void SelectLlmModel(string fileName)
    {
        LlmModelFile = fileName;
        StatusMessage = $"Selected LLM model: {fileName}";
    }

    [RelayCommand]
    private async Task DownloadLlmModelAsync(string fileName)
    {
        var item = LlmModels.FirstOrDefault(l => l.FileName == fileName);
        if (item is null) return;

        item.IsDownloading = true;
        item.StatusText = "Downloading...";
        StatusMessage = $"Downloading LLM {item.DisplayName} model...";

        try
        {
            var progress = new Progress<double>(mb =>
            {
                item.StatusText = $"Downloading ({mb:F1} MB)...";
            });

            await _llmModelManager.EnsureLlmModelDownloadedAsync(fileName, progress);
            StatusMessage = $"LLM {item.DisplayName} model downloaded successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download LLM model {File}", fileName);
            StatusMessage = $"Download failed for {item.DisplayName}. Check internet connection.";
        }
        finally
        {
            item.IsDownloading = false;
            RefreshModelStatuses();
        }
    }

    public void RefreshModelStatuses()
    {
        foreach (var m in WhisperModels)
        {
            bool exists = _modelManager.WhisperModelExists(m.Id);
            bool isActive = string.Equals(m.Id, WhisperModelSize, StringComparison.OrdinalIgnoreCase);

            m.IsDownloaded = exists;
            m.IsActive = isActive;

            if (m.IsDownloading) continue;

            if (isActive && exists)
                m.StatusText = "ACTIVE & READY";
            else if (isActive && !exists)
                m.StatusText = "ACTIVE (Needs Download)";
            else if (exists)
                m.StatusText = "DOWNLOADED";
            else
                m.StatusText = "NOT DOWNLOADED";
        }

        foreach (var l in LlmModels)
        {
            bool exists = _llmModelManager.LlmModelExists(l.FileName);
            bool isActive = string.Equals(l.FileName, LlmModelFile, StringComparison.OrdinalIgnoreCase);

            l.IsDownloaded = exists;
            l.IsActive = isActive;

            if (l.IsDownloading) continue;

            l.StatusText = exists ? (isActive ? "ACTIVE & READY" : "DOWNLOADED") : "NOT DOWNLOADED";
        }

        bool isSelectedDownloaded = !string.IsNullOrWhiteSpace(LlmModelFile) && _llmModelManager.LlmModelExists(LlmModelFile);
        CanEnableAi = isSelectedDownloaded;
        if (!CanEnableAi)
        {
            AiEnabled = false;
            AiStatusCaption = "⚠️ Download and select an LLM model below to enable AI text enhancement.";
        }
        else
        {
            AiStatusCaption = "✓ Selected LLM model is downloaded and ready for AI enhancement.";
        }
    }

    // ── Save / Cancel Commands ────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        var settings = BuildSettings();
        _settingsManager.Save(settings);

        // Update in-memory options instance immediately so running services reflect changes without restart
        _options.Value.General = settings.General;
        _options.Value.Hotkeys = settings.Hotkeys;
        _options.Value.Audio = settings.Audio;
        _options.Value.Speech = settings.Speech;
        _options.Value.Ai = settings.Ai;
        _options.Value.Privacy = settings.Privacy;

        _autoStartService.SetAutoStart(settings.General.StartWithWindows);
        _originalSettings = settings;
        HasChanges = false;
        StatusMessage = "Settings saved.";
        _logger.LogInformation("Settings saved by user. Language: '{Lang}', Model: '{Model}', StartWithWindows: {Value}",
            settings.Speech.Language, settings.Speech.ModelSize, settings.General.StartWithWindows);
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        LoadFromSettings(_originalSettings);
        HasChanges = false;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            string logsDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Vacanam", "Logs");
            System.IO.Directory.CreateDirectory(logsDir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = logsDir,
                UseShellExecute = true,
                Verb = "open"
            });
            _logger.LogInformation("Opened log folder at {Path}", logsDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open log folder.");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        LoadFromSettings(new AppSettings());
        HasChanges = true;
        StatusMessage = "Defaults loaded. Click Save to apply.";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void LoadFromSettings(AppSettings s)
    {
        _originalSettings = s;
        StartWithWindows = s.General.StartWithWindows;
        ShowTrayNotifications = s.General.ShowTrayNotifications;
        DefaultMode = s.General.DefaultMode;
        PushToTalk = s.Hotkeys.PushToTalk;
        EnableVad = s.Audio.EnableVad;
        VadThreshold = s.Audio.VadThreshold;
        SelectedDeviceId = s.Audio.PreferredDeviceId;
        if (_audioRecorder is not null)
        {
            MicVolume = Math.Round(_audioRecorder.MasterVolume * 100.0);
            IsMicMuted = _audioRecorder.IsMuted;
        }
        WhisperModelSize = s.Speech.ModelSize;
        WhisperDevice = s.Speech.Device;
        WhisperLanguage = string.IsNullOrWhiteSpace(s.Speech.Language) ? "en" : s.Speech.Language;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => string.Equals(l.Code, WhisperLanguage, StringComparison.OrdinalIgnoreCase)) ?? AvailableLanguages[0];
        AiEnabled = s.Ai.Enabled;
        LlmModelFile = s.Ai.ModelFile;
        SystemPrompt = string.IsNullOrWhiteSpace(s.Ai.SystemPrompt) ? Vacanam.LLM.Prompts.SystemPrompts.DefaultGrammarFix : s.Ai.SystemPrompt;
        ConservativeMode = s.Ai.ConservativeMode;
        SaveHistory = s.Privacy.SaveHistory;
        MaxHistoryEntries = s.Privacy.MaxHistoryEntries;
        RefreshModelStatuses();
    }

    private AppSettings BuildSettings() => new()
    {
        General = new() { StartWithWindows = StartWithWindows, ShowTrayNotifications = ShowTrayNotifications, DefaultMode = DefaultMode },
        Hotkeys = new() { PushToTalk = PushToTalk, Modifiers = 2, VirtualKey = 0x20 },
        Audio = new() { EnableVad = EnableVad, VadThreshold = VadThreshold, PreferredDeviceId = SelectedDeviceId },
        Speech = new() { ModelSize = WhisperModelSize, Device = WhisperDevice, Language = WhisperLanguage },
        Ai = new() { Enabled = AiEnabled, ModelFile = LlmModelFile, SystemPrompt = SystemPrompt, ConservativeMode = ConservativeMode },
        Privacy = new() { SaveHistory = SaveHistory, MaxHistoryEntries = MaxHistoryEntries }
    };

    public void Reload()
    {
        LoadFromSettings(_settingsManager.Load());
        RefreshModelStatuses();
        _ = RefreshHistoryAsync();
        HasChanges = false;
    }

    public event EventHandler? SaveCompleted;
    public event EventHandler? CancelRequested;
}

/// <summary>Display model for a Whisper model option with download state.</summary>
public sealed partial class WhisperModelItem : ObservableObject
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Size { get; }
    public string Description { get; }

    [ObservableProperty]
    private bool _isDownloaded;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _statusText = "NOT DOWNLOADED";

    public WhisperModelItem(string id, string displayName, string size, string description)
    {
        Id = id;
        DisplayName = displayName;
        Size = size;
        Description = description;
    }
}

/// <summary>Display model for an LLM option with download state.</summary>
public sealed partial class LlmModelItem : ObservableObject
{
    public string FileName { get; }
    public string DisplayName { get; }
    public string Size { get; }
    public string VramRequired { get; }
    public string Description { get; }

    [ObservableProperty]
    private bool _isDownloaded;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _statusText = "NOT DOWNLOADED";

    public LlmModelItem(string fileName, string displayName, string size, string vramRequired, string description)
    {
        FileName = fileName;
        DisplayName = displayName;
        Size = size;
        VramRequired = vramRequired;
        Description = description;
    }
}
