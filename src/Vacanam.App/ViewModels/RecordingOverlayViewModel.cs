using CommunityToolkit.Mvvm.ComponentModel;

using Vacanam.Core.Enums;

namespace Vacanam.App.ViewModels;

/// <summary>
/// ViewModel for the floating recording overlay.
/// Drives visual state (colour, icon, pulse animation) based on VacanamState.
/// </summary>
public sealed partial class RecordingOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private VacanamState _state = VacanamState.Idle;

    [ObservableProperty]
    private double _audioLevel = 0.0;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    [ObservableProperty]
    private bool _isVisible = false;

    [ObservableProperty]
    private bool _isAiTransformMode = false;

    partial void OnIsAiTransformModeChanged(bool value)
    {
        OnPropertyChanged(nameof(StateColorKey));
    }

    partial void OnStateChanged(VacanamState value)
    {
        if (IsAiTransformMode)
        {
            StatusLabel = value switch
            {
                VacanamState.Recording => string.IsNullOrWhiteSpace(StatusLabel) || StatusLabel == "Listening…" ? "🪄 Listening…" : StatusLabel,
                VacanamState.StoppingRecording => "🪄 Processing…",
                VacanamState.Transcribing => "🪄 Transcribing…",
                VacanamState.Processing => "🪄 AI thinking…",
                VacanamState.Inserting => "Inserting…",
                VacanamState.Completed => "Done ✓",
                VacanamState.Error => "Error",
                _ => string.Empty
            };
        }
        else
        {
            StatusLabel = value switch
            {
                VacanamState.Recording => "Listening…",
                VacanamState.StoppingRecording => "Processing…",
                VacanamState.Transcribing => "Transcribing…",
                VacanamState.Processing => "AI mode…",
                VacanamState.Inserting => "Inserting…",
                VacanamState.Completed => "Done ✓",
                VacanamState.Error => "Error",
                _ => string.Empty
            };
        }

        IsVisible = value is not VacanamState.Idle;
        OnPropertyChanged(nameof(StateColorKey));
    }

    // Returns a colour key name for the state indicator ring.
    public string StateColorKey => State switch
    {
        VacanamState.Recording => IsAiTransformMode ? "BrandBrush" : "RecordingBrush",
        VacanamState.Transcribing or VacanamState.Processing => "BrandBrush",
        VacanamState.Completed => "SuccessBrush",
        VacanamState.Error => "ErrorBrush",
        _ => "TextDisabledBrush"
    };

    partial void OnAudioLevelChanged(double value)
    {
        // Overlay ring grows with audio level — binding picks this up
    }
}
