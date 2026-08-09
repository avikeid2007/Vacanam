using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Infrastructure.Configuration;

namespace Vacanam.App.Views;

/// <summary>
/// Minimal, modern Windows 11 splash launch banner displayed on app startup.
/// Auto-downloads the initial Ultra Fast (tiny) Whisper model if no model exists on disk.
/// </summary>
public partial class LaunchBannerWindow : Window
{
    private readonly AppSettings _settings;
    private readonly IModelManager? _modelManager;
    private readonly SettingsManager? _settingsManager;
    private readonly DispatcherTimer _closeTimer;
    private bool _isClosing;
    private bool _isDownloading;

    public LaunchBannerWindow(AppSettings settings, IModelManager? modelManager = null, SettingsManager? settingsManager = null)
    {
        InitializeComponent();

        _settings = settings;
        _modelManager = modelManager;
        _settingsManager = settingsManager;

        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(3000)
        };
        _closeTimer.Tick += OnTimerTick;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
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
                _settings.Speech.ModelSize = "tiny";
                if (_settingsManager is not null)
                {
                    _settingsManager.Save(_settings);
                }

                // Setup Complete!
                _isDownloading = false;
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)); // Emerald Green
                TxtBadgeText.Text = "READY";
                TxtSubtitle.Text = "Initial setup complete! Selected Ultra Fast model.";
                TxtHotkeyTitle.Text = "Global Dictation Hotkey";
                TxtHotkeyDesc.Text = "Hold key combination to dictate into any active window";

                // Start countdown progress animation & 2s close timer
                if (Resources["ProgressAnimation"] is Storyboard progressSb)
                {
                    progressSb.Begin(this);
                }

                _closeTimer.Interval = TimeSpan.FromMilliseconds(2000);
                _closeTimer.Start();
            }
            catch (Exception ex)
            {
                _isDownloading = false;
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)); // Red
                TxtBadgeText.Text = "SETUP FAILED";
                TxtSubtitle.Text = "Download failed. Please check internet connection.";
                TxtHotkeyDesc.Text = ex.Message;

                _closeTimer.Interval = TimeSpan.FromMilliseconds(4000);
                _closeTimer.Start();
            }
        }
        else
        {
            // Model exists: Start countdown progress line & 3s close timer
            if (Resources["ProgressAnimation"] is Storyboard progressSb)
            {
                progressSb.Begin(this);
            }
            _closeTimer.Start();
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _closeTimer.Stop();
        FadeOutAndClose();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isDownloading) return; // Don't close while downloading initial setup
        _closeTimer.Stop();
        FadeOutAndClose();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_isDownloading) return; // Don't close while downloading initial setup
        _closeTimer.Stop();
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
            Duration = new Duration(TimeSpan.FromMilliseconds(250))
        };

        fadeAnimation.Completed += (s, e) => Close();
        BeginAnimation(OpacityProperty, fadeAnimation);
    }
}
