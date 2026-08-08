using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Enums;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Infrastructure.Configuration;

namespace Vacanam.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window. Manages all settings categories,
/// model download statuses, and live model selection.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsManager _settingsManager;
    private readonly IModelManager _modelManager;
    private readonly ILogger<SettingsViewModel> _logger;
    private AppSettings _originalSettings = new();

    public SettingsViewModel(
        SettingsManager settingsManager,
        IModelManager modelManager,
        ILogger<SettingsViewModel> logger)
    {
        _settingsManager = settingsManager;
        _modelManager = modelManager;
        _logger = logger;

        LoadFromSettings(_settingsManager.Load());
        RefreshModelStatuses();
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
        new("tiny",      "Tiny",     "~75 MB",   "Fastest — good for short phrases"),
        new("small",     "Small",    "~466 MB",  "Recommended — balanced speed and accuracy"),
        new("medium",    "Medium",   "~1.5 GB",  "Better accuracy — requires more RAM"),
        new("large-v3",  "Large v3", "~3.1 GB",  "Best accuracy — GPU strongly recommended"),
    ];

    [ObservableProperty]
    private string _whisperDevice = "Auto";

    public IReadOnlyList<string> WhisperDevices { get; } = ["Auto", "CPU", "CUDA"];

    [ObservableProperty]
    private string _whisperLanguage = "auto";

    // ── AI Tab ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _aiEnabled = false;

    [ObservableProperty]
    private string _llmModelFile = string.Empty;

    public List<LlmModelItem> LlmModels { get; } =
    [
        new("phi-3.5-mini-instruct-Q4_K_M.gguf",   "Phi-3.5 Mini",   "~2.2 GB", "2 GB+ VRAM",  "Microsoft — Fast, great grammar correction"),
        new("llama-3.2-3b-Q4_K_M.gguf",             "Llama 3.2 3B",   "~1.8 GB", "2 GB+ VRAM",  "Meta — Balanced quality and speed"),
        new("gemma-2-2b-it-Q4_K_M.gguf",            "Gemma 2 2B",     "~1.6 GB", "2 GB+ VRAM",  "Google — Compact and capable"),
    ];

    [ObservableProperty]
    private bool _conservativeMode = true;

    // ── Privacy Tab ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _saveHistory = false;

    [ObservableProperty]
    private int _maxHistoryEntries = 1000;

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
        StatusMessage = $"Selected Whisper model: {modelId}";
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

            if (_modelManager is Vacanam.Speech.Model.ModelManager manager)
            {
                await manager.EnsureWhisperModelDownloadedAsync(modelId, progress);
            }

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
            bool exists = _modelManager.LlmModelExists(l.FileName);
            bool isActive = string.Equals(l.FileName, LlmModelFile, StringComparison.OrdinalIgnoreCase);

            l.IsDownloaded = exists;
            l.IsActive = isActive;
            l.StatusText = exists ? (isActive ? "ACTIVE" : "DOWNLOADED") : "NOT DOWNLOADED";
        }
    }

    // ── Save / Cancel Commands ────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        var settings = BuildSettings();
        _settingsManager.Save(settings);
        _originalSettings = settings;
        HasChanges = false;
        StatusMessage = "Settings saved.";
        _logger.LogInformation("Settings saved by user.");
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
        WhisperModelSize = s.Speech.ModelSize;
        WhisperDevice = s.Speech.Device;
        WhisperLanguage = s.Speech.Language;
        AiEnabled = s.Ai.Enabled;
        LlmModelFile = s.Ai.ModelFile;
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
        Ai = new() { Enabled = AiEnabled, ModelFile = LlmModelFile, ConservativeMode = ConservativeMode },
        Privacy = new() { SaveHistory = SaveHistory, MaxHistoryEntries = MaxHistoryEntries }
    };

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
