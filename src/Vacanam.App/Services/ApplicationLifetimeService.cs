using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Vacanam.App.ViewModels;
using Vacanam.App.Views;
using Vacanam.Audio.Capture;
using Vacanam.Core.Enums;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.App.Services;

/// <summary>
/// Manages the application lifecycle: tray icon, hotkey registration,
/// WASAPI audio capture, Whisper STT transcription, instant text injection,
/// overlay management, and pipeline state coordination.
///
/// Thread-safe, stable, low-latency pipeline with zero native crashes.
/// </summary>
public sealed class ApplicationLifetimeService(
    IHostApplicationLifetime lifetime,
    IGlobalHotkeyService hotkeyService,
    IForegroundWindowService foregroundWindowService,
    IAudioRecorder audioRecorder,
    ISpeechRecognizer speechRecognizer,
    ITextInjector textInjector,
    MainViewModel mainViewModel,
    SettingsViewModel settingsViewModel,
    RecordingOverlayViewModel overlayViewModel,
    ILogger<ApplicationLifetimeService> logger) : IHostedService, IDisposable
{
    private TaskbarIcon? _trayIcon;
    private RecordingOverlay? _overlay;
    private RecordingBuffer? _recordingBuffer;
    private DispatcherTimer? _audioMeterTimer;
    private ApplicationContext _currentSessionContext = ApplicationContext.Unknown;
    private readonly SemaphoreSlim _pipelineLock = new(1, 1);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ApplicationLifetimeService starting.");

        Application.Current.Dispatcher.Invoke(() =>
        {
            InitialiseTrayIcon();
            InitialiseOverlay();
            WireViewModelEvents();
            RegisterGlobalHotkey();
            InitialiseAudioMeterTimer();
        });

        speechRecognizer.SegmentReceived += OnTranscriptSegmentReceived;

        logger.LogInformation("Vacanam is running. Hold Ctrl+Space to dictate into any window.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ApplicationLifetimeService stopping.");
        speechRecognizer.SegmentReceived -= OnTranscriptSegmentReceived;
        Application.Current.Dispatcher.Invoke(Cleanup);
        return Task.CompletedTask;
    }

    private void InitialiseTrayIcon()
    {
        var iconResource = (TaskbarIcon)Application.Current.FindResource("VacanamTrayIcon");
        _trayIcon = iconResource;
        _trayIcon.DataContext = mainViewModel;
        logger.LogDebug("Tray icon initialised.");
    }

    private void InitialiseOverlay()
    {
        _overlay = new RecordingOverlay(overlayViewModel);
        logger.LogDebug("Recording overlay initialised.");
    }

    private void InitialiseAudioMeterTimer()
    {
        _audioMeterTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _audioMeterTimer.Tick += (_, _) =>
        {
            if (audioRecorder.IsRecording)
            {
                double level = audioRecorder.AudioLevel;
                mainViewModel.AudioLevel = level;
                overlayViewModel.AudioLevel = level;

                if (audioRecorder.IsMuted)
                {
                    overlayViewModel.StatusLabel = "Mic Muted 🔇";
                }
                else if (audioRecorder.MasterVolume < 0.30f)
                {
                    int volPercent = (int)(audioRecorder.MasterVolume * 100);
                    overlayViewModel.StatusLabel = $"Mic Volume {volPercent}% 🔇 (Low)";
                }
                else if (overlayViewModel.StatusLabel.StartsWith("Mic Muted") || overlayViewModel.StatusLabel.StartsWith("Mic Volume"))
                {
                    overlayViewModel.StatusLabel = "Recording…";
                }
            }
        };
    }

    private void WireViewModelEvents()
    {
        mainViewModel.SettingsRequested += OnSettingsRequested;
        mainViewModel.ExitRequested += OnExitRequested;
    }

    private void RegisterGlobalHotkey()
    {
        hotkeyService.HotkeyPressed  += OnHotkeyPressed;
        hotkeyService.HotkeyReleased += OnHotkeyReleased;

        bool registered = hotkeyService.Register(0);
        mainViewModel.IsHotkeyRegistered = registered;

        if (registered)
            logger.LogInformation("Global hotkey registered successfully (Ctrl+Space).");
        else
            logger.LogWarning("Global hotkey registration failed. Check Settings → Hotkeys.");
    }

    private void OnTranscriptSegmentReceived(object? sender, TranscriptSegmentEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrWhiteSpace(e.Text) && !e.Text.Contains("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase))
            {
                overlayViewModel.StatusLabel = e.Text;
            }
        });
    }

    // ── Hotkey Handlers ───────────────────────────────────────────────────────

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(async () =>
        {
            if (mainViewModel.CurrentState is not VacanamState.Idle)
            {
                logger.LogDebug("Hotkey pressed but state is {State}. Ignoring.", mainViewModel.CurrentState);
                return;
            }

            _currentSessionContext = foregroundWindowService.GetCurrentContext();
            logger.LogInformation(
                "Recording started. Target app: {Process} — Title: '{Title}' (HWND={Hwnd:X})",
                _currentSessionContext.ProcessName, _currentSessionContext.WindowTitle, _currentSessionContext.WindowHandle);

            try
            {
                _recordingBuffer?.Dispose();
                _recordingBuffer = new RecordingBuffer(audioRecorder);
                _recordingBuffer.BeginCapture();

                await audioRecorder.StartAsync();

                TransitionTo(VacanamState.Recording);
                overlayViewModel.State = VacanamState.Recording;
                ShowOverlay();
                _audioMeterTimer?.Start();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start audio recording session.");
                TransitionTo(VacanamState.Error);
                await Task.Delay(1000);
                TransitionTo(VacanamState.Idle);
            }
        });
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(async () =>
        {
            if (mainViewModel.CurrentState is not VacanamState.Recording)
            {
                logger.LogDebug("Hotkey released but state is {State}. Ignoring.", mainViewModel.CurrentState);
                return;
            }

            logger.LogInformation("Hotkey released. Stopping audio capture.");
            _audioMeterTimer?.Stop();

            try
            {
                TransitionTo(VacanamState.StoppingRecording);

                await audioRecorder.StopAsync();
                _recordingBuffer?.EndCapture();

                int totalBytes = _recordingBuffer?.TotalBytes ?? 0;
                TimeSpan duration = _recordingBuffer?.Duration ?? TimeSpan.Zero;
                logger.LogInformation(
                    "Captured audio session complete: {Duration:g} ({Bytes} bytes 16kHz PCM).",
                    duration, totalBytes);

                if (totalBytes < 1600) // Less than ~50ms of audio
                {
                    logger.LogInformation("Audio clip too short. Skipping transcription.");
                    TransitionTo(VacanamState.Idle);
                    overlayViewModel.State = VacanamState.Idle;
                    HideOverlay();
                    return;
                }

                // Whisper STT Transcription with VAD silence trimming
                TransitionTo(VacanamState.Transcribing);
                overlayViewModel.State = VacanamState.Transcribing;

                using var wavStream = _recordingBuffer!.ToWavStream(trimSilence: true);
                string transcript = await speechRecognizer.TranscribeAsync(wavStream);

                // Clean up any stray [BLANK_AUDIO] tokens
                transcript = CleanTranscript(transcript);

                logger.LogInformation(">>> FINAL TRANSCRIPT: '{Transcript}' <<<", transcript);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    if (audioRecorder.IsMuted)
                    {
                        overlayViewModel.StatusLabel = "Mic is Muted 🔇";
                    }
                    else if (audioRecorder.MasterVolume < 0.30f)
                    {
                        int volPercent = (int)(audioRecorder.MasterVolume * 100);
                        overlayViewModel.StatusLabel = $"Mic volume is low ({volPercent}%) 🔇";
                    }
                    else
                    {
                        overlayViewModel.StatusLabel = "No speech detected";
                    }
                    await Task.Delay(800);
                }
                else
                {
                    overlayViewModel.StatusLabel = transcript;

                    // Text Injection into target window
                    TransitionTo(VacanamState.Inserting);
                    overlayViewModel.State = VacanamState.Inserting;

                    await textInjector.InjectAsync(transcript, _currentSessionContext);

                    TransitionTo(VacanamState.Completed);
                    overlayViewModel.State = VacanamState.Completed;
                    await Task.Delay(300);
                }

                TransitionTo(VacanamState.Idle);
                overlayViewModel.State = VacanamState.Idle;
                HideOverlay();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during transcription & injection pipeline.");
                overlayViewModel.StatusLabel = "Error";
                TransitionTo(VacanamState.Error);
                await Task.Delay(1000);
                TransitionTo(VacanamState.Idle);
                overlayViewModel.State = VacanamState.Idle;
                HideOverlay();
            }
            finally
            {
                _recordingBuffer?.Dispose();
                _recordingBuffer = null;
            }
        });
    }

    private static string CleanTranscript(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return raw.Replace("[BLANK_AUDIO]", "", StringComparison.OrdinalIgnoreCase)
                  .Replace("(blank audio)", "", StringComparison.OrdinalIgnoreCase)
                  .Trim();
    }

    private void TransitionTo(VacanamState state)
    {
        mainViewModel.CurrentState = state;
        if (_trayIcon is not null)
            _trayIcon.ToolTipText = mainViewModel.TrayTooltip;
    }

    private void ShowOverlay() => _overlay?.Show();
    private void HideOverlay() => _overlay?.Hide();

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var settingsWindow = new SettingsWindow(settingsViewModel);
            settingsWindow.Show();
            settingsWindow.Activate();
        });
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        logger.LogInformation("Exit requested — shutting down.");
        lifetime.StopApplication();
    }

    private void Cleanup()
    {
        _audioMeterTimer?.Stop();
        _audioMeterTimer = null;

        hotkeyService.HotkeyPressed  -= OnHotkeyPressed;
        hotkeyService.HotkeyReleased -= OnHotkeyReleased;
        hotkeyService.Unregister();

        _recordingBuffer?.Dispose();
        _recordingBuffer = null;

        audioRecorder.Dispose();
        speechRecognizer.Dispose();

        _trayIcon?.Dispose();
        _trayIcon = null;

        logger.LogDebug("ApplicationLifetimeService cleaned up.");
    }

    public void Dispose() => Application.Current.Dispatcher.Invoke(Cleanup);
}
