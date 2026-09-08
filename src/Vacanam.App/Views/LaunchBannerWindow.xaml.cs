using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Infrastructure.Configuration;

namespace Vacanam.App.Views;

/// <summary>
/// First-Run Onboarding and Quick Start Guide window displayed on startup or from system tray.
/// Features interactive scratchpad for testing voice typing, core shortcut cheat sheet,
/// tray guidance, and persistent onboarding completion state.
/// </summary>
public partial class LaunchBannerWindow : Window
{
    private const string DefaultPlaceholder = "Click here, hold Ctrl + Space, and speak a test phrase...";

    private readonly AppSettings _settings;
    private readonly IModelManager? _modelManager;
    private readonly SettingsManager? _settingsManager;
    private bool _isClosing;
    private bool _isDownloading;
    private bool _hasClearedPlaceholder;

    public LaunchBannerWindow(AppSettings settings, IModelManager? modelManager = null, SettingsManager? settingsManager = null)
    {
        InitializeComponent();

        _settings = settings;
        _modelManager = modelManager;
        _settingsManager = settingsManager;

        ChkShowOnStartup.IsChecked = _settings.General.ShowLaunchBannerOnStartup;
        TxtTestScratchpad.Text = DefaultPlaceholder;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Dynamically update engine pill labels based on settings and model state
        if (!string.IsNullOrWhiteSpace(_settings.Speech?.ModelSize))
        {
            TxtWhisperEngine.Text = $"⚡ Whisper ({_settings.Speech.ModelSize})";
        }

        if (_modelManager is not null && !string.IsNullOrWhiteSpace(_settings.Ai?.ModelFile))
        {
            bool llmExists = _modelManager.LlmModelExists(_settings.Ai.ModelFile);
            TxtLlmEngine.Text = llmExists ? "🧠 Local LLM (Ready)" : "🧠 Local LLM";
        }

        // Trigger Entry Window animation
        if (Resources["WindowLoadAnimation"] is Storyboard loadSb)
        {
            loadSb.Begin(this);
        }

        // Trigger continuous subtle status pulse animation
        if (Resources["StatusPulseAnimation"] is Storyboard pulseSb)
        {
            pulseSb.Begin(this);
        }

        // Check if ANY Whisper model is downloaded on disk
        bool hasModel = _modelManager is not null &&
            (_modelManager.WhisperModelExists("tiny") ||
             _modelManager.WhisperModelExists("small") ||
             _modelManager.WhisperModelExists("medium") ||
             _modelManager.WhisperModelExists("large-v3"));

        if (!hasModel && _modelManager is not null)
        {
            _isDownloading = true;

            // Update UI for Initial Setup state
            StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)); // Amber
            TxtBadgeText.Text = "SETTING UP SYSTEM";
            TxtSubtitle.Text = "Downloading initial speech engine (Ultra Fast ~75 MB)... Please wait.";
            TxtHotkeyTitle.Text = "Setting up Vacanam Speech Engine...";
            TxtHotkeyDesc.Text = "Downloading Ultra Fast model for instant voice typing...";

            try
            {
                var progress = new Progress<double>(mb =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtHotkeyDesc.Text = $"Downloading Ultra Fast model... {mb:F1} MB downloaded";
                    });
                });

                await _modelManager.EnsureWhisperModelDownloadedAsync("tiny", progress);

                // Set selected model size to "tiny"
                if (_settings.Speech is not null)
                {
                    _settings.Speech.ModelSize = "tiny";
                    TxtWhisperEngine.Text = "⚡ Whisper (tiny)";
                }
                if (_settingsManager is not null)
                {
                    _settingsManager.Save(_settings);
                }

                // Setup Complete!
                _isDownloading = false;
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)); // Emerald Green
                TxtBadgeText.Text = "READY";
                TxtSubtitle.Text = "Initial setup complete! Selected Ultra Fast model.";
                TxtHotkeyTitle.Text = "Voice Typing / Dictation";
                TxtHotkeyDesc.Text = "Hold to transcribe speech directly into any focused application";
            }
            catch (Exception ex)
            {
                _isDownloading = false;
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)); // Red
                TxtBadgeText.Text = "SETUP FAILED";
                TxtSubtitle.Text = "Download failed. Please check internet connection.";
                TxtHotkeyDesc.Text = ex.Message;
            }
        }
    }

    private void TxtTestScratchpad_GotFocus(object sender, RoutedEventArgs e)
    {
        if (!_hasClearedPlaceholder && TxtTestScratchpad.Text == DefaultPlaceholder)
        {
            TxtTestScratchpad.Text = string.Empty;
            _hasClearedPlaceholder = true;
        }
    }

    private void BtnClearScratchpad_Click(object sender, RoutedEventArgs e)
    {
        TxtTestScratchpad.Text = string.Empty;
        _hasClearedPlaceholder = true;
        TxtTestScratchpad.Focus();
    }

    public event EventHandler? SettingsRequested;

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        CompleteOnboardingAndClose();
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        CompleteOnboardingAndClose();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_isDownloading) return;

        if (e.Key == Key.Escape || (e.Key == Key.Enter && !TxtTestScratchpad.IsFocused))
        {
            CompleteOnboardingAndClose();
            e.Handled = true;
        }
    }

    private void CompleteOnboardingAndClose()
    {
        if (_isClosing) return;

        try
        {
            // Mark onboarding as completed and persist startup banner preference
            _settings.General.HasCompletedOnboarding = true;
            _settings.General.ShowLaunchBannerOnStartup = ChkShowOnStartup.IsChecked ?? true;

            _settingsManager?.Save(_settings);
        }
        catch
        {
            // Fail gracefully if settings file could not be saved
        }

        FadeOutAndClose();
    }

    private void FadeOutAndClose()
    {
        if (_isClosing) return;
        _isClosing = true;

        var fadeAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(220))
        };

        fadeAnimation.Completed += (s, e) => Close();
        BeginAnimation(OpacityProperty, fadeAnimation);
    }
}
