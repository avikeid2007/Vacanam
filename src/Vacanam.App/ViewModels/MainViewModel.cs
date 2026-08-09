using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Enums;

namespace Vacanam.App.ViewModels;

/// <summary>
/// Main application ViewModel. Owns the application state machine,
/// tray icon state, and command routing. No business logic lives here.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
        _logger.LogInformation("Vacanam started. Current state: {State}", CurrentState);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private VacanamState _currentState = VacanamState.Idle;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isRecording = false;

    [ObservableProperty]
    private double _audioLevel = 0.0;

    // ── Hotkey Active Indicator ───────────────────────────────────────────────

    [ObservableProperty]
    private bool _isHotkeyRegistered = false;

    // ── Tray Tooltip ─────────────────────────────────────────────────────────

    public string TrayTooltip => CurrentState switch
    {
        VacanamState.Idle => "Vacanam — Ready (Ctrl+Space to record)",
        VacanamState.Recording => "Vacanam — Recording…",
        VacanamState.Transcribing => "Vacanam — Transcribing…",
        VacanamState.Processing => "Vacanam — AI processing…",
        VacanamState.Inserting => "Vacanam — Inserting text…",
        VacanamState.Error => "Vacanam — Error occurred",
        _ => "Vacanam"
    };

    partial void OnCurrentStateChanged(VacanamState value)
    {
        OnPropertyChanged(nameof(TrayTooltip));
        IsRecording = value == VacanamState.Recording;
        StatusText = value switch
        {
            VacanamState.Idle => "Ready",
            VacanamState.StartingRecording => "Starting…",
            VacanamState.Recording => "Recording…",
            VacanamState.StoppingRecording => "Stopping…",
            VacanamState.Transcribing => "Transcribing…",
            VacanamState.Processing => "AI processing…",
            VacanamState.Inserting => "Inserting…",
            VacanamState.Completed => "Done",
            VacanamState.Error => "Error",
            _ => string.Empty
        };
        _logger.LogDebug("State changed to {State}", value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenSettings()
    {
        _logger.LogDebug("OpenSettings command invoked.");
        // SettingsWindow will be opened by ApplicationLifetimeService
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ExitApplication()
    {
        _logger.LogInformation("Exit requested by user.");
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        if (CurrentState == VacanamState.Recording)
        {
            _logger.LogDebug("Manual stop recording invoked.");
            StopRecordingRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (CurrentState == VacanamState.Idle)
        {
            _logger.LogDebug("Manual start recording invoked.");
            StartRecordingRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    // ── Events (for ApplicationLifetimeService) ───────────────────────────────

    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? StartRecordingRequested;
    public event EventHandler? StopRecordingRequested;
}
