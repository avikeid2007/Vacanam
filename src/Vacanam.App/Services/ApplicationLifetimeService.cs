using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Vacanam.App.ViewModels;
using Vacanam.App.Views;
using Vacanam.Audio.Capture;
using Vacanam.Core.Enums;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Infrastructure.Configuration;

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
    ITextProcessor textProcessor,
    ITextInjector textInjector,
    IClipboardService clipboardService,
    ITranscriptHistoryRepository historyRepository,
    IVoiceCommandProcessor voiceCommandProcessor,
    Vacanam.Speech.Punctuation.SmartPunctuationProcessor smartPunctuationProcessor,
    IModelManager modelManager,
    SettingsManager settingsManager,
    IAutoStartService autoStartService,
    IOptions<AppSettings> settings,
    MainViewModel mainViewModel,
    SettingsViewModel settingsViewModel,
    RecordingOverlayViewModel overlayViewModel,
    IAudioFeedbackService audioFeedbackService,
    ILogger<ApplicationLifetimeService> logger) : IHostedService, IDisposable
{
    private TaskbarIcon? _trayIcon;
    private RecordingOverlay? _overlay;
    private RecordingBuffer? _recordingBuffer;
    private DispatcherTimer? _audioMeterTimer;
    private ApplicationContext _currentSessionContext = ApplicationContext.Unknown;
    private readonly SemaphoreSlim _pipelineLock = new(1, 1);
    private bool _stopRequestedDuringStart;
    private bool _isAiTransformMode;
    private string? _capturedSelectedText;

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
            ShowLaunchBanner();
        });

        // Sync Windows Startup Registry state
        autoStartService.SetAutoStart(settings.Value.General.StartWithWindows);

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

    private void ShowLaunchBanner()
    {
        try
        {
            var currentSettings = settings.Value;
            bool shouldShow = !currentSettings.General.HasCompletedOnboarding || currentSettings.General.ShowLaunchBannerOnStartup;
            if (!shouldShow)
            {
                logger.LogInformation("Launch onboarding banner skipped (onboarding already completed).");
                return;
            }

            var banner = new LaunchBannerWindow(currentSettings, modelManager, settingsManager);
            banner.SettingsRequested += OnSettingsRequested;
            banner.Show();
            logger.LogInformation("Launch onboarding banner displayed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to display launch banner.");
        }
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
        mainViewModel.QuickStartRequested += OnQuickStartRequested;
        mainViewModel.SettingsRequested += OnSettingsRequested;
        mainViewModel.ExitRequested += OnExitRequested;
        mainViewModel.StartRecordingRequested += OnHotkeyPressed;
        mainViewModel.StopRecordingRequested += OnHotkeyReleased;
    }

    private void RegisterGlobalHotkey()
    {
        hotkeyService.HotkeyPressed  += OnHotkeyPressed;
        hotkeyService.HotkeyReleased += OnHotkeyReleased;
        hotkeyService.AiTransformHotkeyPressed  += OnAiTransformHotkeyPressed;
        hotkeyService.AiTransformHotkeyReleased += OnAiTransformHotkeyReleased;

        bool registered = hotkeyService.Register(0);
        mainViewModel.IsHotkeyRegistered = registered;

        if (registered)
        {
            logger.LogInformation("Global hotkeys registered successfully (Ctrl+Space dictation, Shift+Space Ask AI).");
        }
        else
        {
            logger.LogWarning("Global hotkey registration failed — shortcut is in use by another app.");
            // Notify user via balloon tip — they can still use tray ▶ Start Dictation
            _trayIcon?.ShowBalloonTip(
                "Hotkey Conflict",
                "A hotkey is already used by another app. You can change the hotkeys in Settings.",
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
        }
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
        StartRecordingSession(isAiTransform: false);
    }

    private void OnAiTransformHotkeyPressed(object? sender, EventArgs e)
    {
        StartRecordingSession(isAiTransform: true);
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        StopRecordingSession();
    }

    private void OnAiTransformHotkeyReleased(object? sender, EventArgs e)
    {
        StopRecordingSession();
    }

    private async void StartRecordingSession(bool isAiTransform)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (mainViewModel.CurrentState is not VacanamState.Idle)
            {
                logger.LogDebug("Hotkey pressed but state is {State}. Ignoring.", mainViewModel.CurrentState);
                return;
            }

            _isAiTransformMode = isAiTransform;
            _capturedSelectedText = null;
            _stopRequestedDuringStart = false;
            TransitionTo(VacanamState.StartingRecording);
            overlayViewModel.State = VacanamState.StartingRecording;
            overlayViewModel.IsAiTransformMode = isAiTransform;

            _currentSessionContext = foregroundWindowService.GetCurrentContext();
            logger.LogInformation(
                "Recording started ({Mode}). Target app: {Process} — Title: '{Title}' (HWND={Hwnd:X})",
                isAiTransform ? "Ask AI / Voice Transform" : "Dictation",
                _currentSessionContext.ProcessName, _currentSessionContext.WindowTitle, _currentSessionContext.WindowHandle);

            if (isAiTransform)
            {
                _capturedSelectedText = await CaptureSelectedTextAsync(_currentSessionContext.WindowHandle);
                overlayViewModel.StatusLabel = !string.IsNullOrWhiteSpace(_capturedSelectedText)
                    ? "🪄 Transform Selection…"
                    : "🪄 Ask AI…";
            }

            try
            {
                _recordingBuffer?.Dispose();
                _recordingBuffer = new RecordingBuffer(audioRecorder);
                _recordingBuffer.BeginCapture();

                await audioRecorder.StartAsync();

                if (_stopRequestedDuringStart)
                {
                    logger.LogInformation("Hotkey was released during startup. Immediately stopping.");
                    _stopRequestedDuringStart = false;
                    await StopRecordingAndProcessAsync();
                    return;
                }

                audioFeedbackService.Play(AudioCue.Start);

                TransitionTo(VacanamState.Recording);
                overlayViewModel.State = VacanamState.Recording;
                ShowOverlay();
                _audioMeterTimer?.Start();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start audio recording session.");
                audioFeedbackService.Play(AudioCue.Error);
                TransitionTo(VacanamState.Error);
                await Task.Delay(1000);
                TransitionTo(VacanamState.Idle);
            }
        }).Task.Unwrap();
    }

    private async void StopRecordingSession()
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (mainViewModel.CurrentState is VacanamState.StartingRecording)
            {
                logger.LogInformation("Hotkey released while starting recording. Flagging stop request.");
                _stopRequestedDuringStart = true;
                return;
            }

            if (mainViewModel.CurrentState is not VacanamState.Recording)
            {
                logger.LogDebug("Hotkey released but state is {State}. Ignoring.", mainViewModel.CurrentState);
                return;
            }

            await StopRecordingAndProcessAsync();
        }).Task.Unwrap();
    }

    private async Task<string?> CaptureSelectedTextAsync(nint targetHwnd)
    {
        try
        {
            // Suppress hold poller during simulation to prevent false key-up triggers when releasing Shift
            hotkeyService.SuppressHoldDetection(true);

            object? backup = null;
            try
            {
                backup = await clipboardService.BackupAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to backup clipboard prior to text selection probe.");
            }

            string sentinel = $"__VACANAM_SENTINEL_{Guid.NewGuid():N}__";
            await clipboardService.SetTextAsync(sentinel);

            // Send Ctrl+C with modifier release to reliably capture selection in VS Code, Outlook, etc.
            bool isPushToTalk = settings.Value.Hotkeys.PushToTalk;
            await Vacanam.Windows.Interop.KeySimulator.CopySelectionAsync(
                targetHwnd,
                isPushToTalk: isPushToTalk);

            // Poll clipboard for up to 200ms (8 * 25ms) waiting for target app to process copy
            string? capturedText = null;
            for (int i = 0; i < 8; i++)
            {
                await Task.Delay(25);
                string? clipboardText = await clipboardService.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText) && !string.Equals(clipboardText, sentinel, StringComparison.Ordinal))
                {
                    capturedText = clipboardText;
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(capturedText))
            {
                logger.LogInformation("Captured {Length} chars of selected text from HWND={Hwnd:X}", capturedText.Length, targetHwnd);
            }
            else
            {
                logger.LogInformation("No text selected in target window. Entering Direct Ask AI mode.");
            }

            // Restore user clipboard immediately
            if (backup is not null)
            {
                try
                {
                    await clipboardService.RestoreAsync(backup);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to restore clipboard after selection probe.");
                }
            }
            else
            {
                // Clear sentinel so it does not linger on user's clipboard
                try
                {
                    await clipboardService.SetTextAsync(string.Empty);
                }
                catch
                {
                    // Best effort
                }
            }

            return capturedText;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to probe selected text from target window.");
            return null;
        }
        finally
        {
            hotkeyService.SuppressHoldDetection(false);
        }
    }

    private async Task StopRecordingAndProcessAsync()
    {
        logger.LogInformation("Hotkey released. Stopping audio capture.");
        audioFeedbackService.Play(AudioCue.Stop);
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
                audioFeedbackService.Play(AudioCue.Error);
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
                audioFeedbackService.Play(AudioCue.Error);
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
                string rawTranscript = transcript;
                bool wasActuallyEnhanced = false;
                bool isAiEnabled = settings.Value.Ai.Enabled;
                bool wasSnippetExpanded = false;

                if (_isAiTransformMode)
                {
                    bool hasSelection = !string.IsNullOrWhiteSpace(_capturedSelectedText);
                    logger.LogInformation(
                        "Voice Transform mode active. Instruction: '{Transcript}', hasSelection: {HasSelection}",
                        transcript, hasSelection);

                    TransitionTo(VacanamState.Processing);
                    overlayViewModel.State = VacanamState.Processing;
                    overlayViewModel.StatusLabel = hasSelection ? "🪄 Transforming…" : "🪄 Thinking…";

                    try
                    {
                        string transformed = await textProcessor.TransformAsync(
                            transcript,
                            _capturedSelectedText,
                            _currentSessionContext);

                        if (!string.IsNullOrWhiteSpace(transformed))
                        {
                            logger.LogInformation(">>> TRANSFORMED OUTPUT: '{Output}' <<<", transformed);
                            transcript = transformed;
                            wasActuallyEnhanced = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Voice Transform failed. Using raw transcript.");
                    }

                    rawTranscript = hasSelection
                        ? $"[Transform: {rawTranscript}] {_capturedSelectedText}"
                        : $"[Ask AI] {rawTranscript}";
                }
                else
                {
                    // 1. Voice Command & Snippet Detection
                    if (settings.Value.VoiceCommands.Enabled)
                    {
                        var cmdResult = await voiceCommandProcessor.ProcessAsync(transcript, _currentSessionContext);
                        if (cmdResult.WasCommand)
                        {
                            if (string.IsNullOrEmpty(cmdResult.ProcessedText))
                            {
                                // Standalone Action Command executed (Select All, Copy, Undo, etc.)
                                logger.LogInformation("Voice Action Command executed: {Command}", cmdResult.CommandName);
                                audioFeedbackService.Play(AudioCue.Success);
                                overlayViewModel.StatusLabel = $"⚡ {cmdResult.CommandName}";
                                TransitionTo(VacanamState.Completed);
                                overlayViewModel.State = VacanamState.Completed;
                                await Task.Delay(400);
                                TransitionTo(VacanamState.Idle);
                                overlayViewModel.State = VacanamState.Idle;
                                HideOverlay();
                                return;
                            }
                            else
                            {
                                // Custom Snippet Macro expanded
                                logger.LogInformation("Voice Snippet expanded: {Command}", cmdResult.CommandName);
                                transcript = cmdResult.ProcessedText;
                                wasSnippetExpanded = true;
                            }
                        }
                    }

                    // Only apply smart punctuation & AI rewrite if it wasn't a custom snippet expansion
                    if (!wasSnippetExpanded)
                    {
                        // 2. Smart Verbal Punctuation Formatting
                        if (settings.Value.VoiceCommands.EnableSmartPunctuation)
                        {
                            string formatted = smartPunctuationProcessor.Format(transcript);
                            if (!string.IsNullOrWhiteSpace(formatted))
                            {
                                transcript = formatted;
                            }
                        }

                        // 3. AI Text Enhancement
                        if (isAiEnabled)
                        {
                            try
                            {
                                logger.LogInformation("AI text enhancement enabled. Processing transcript with LLM model '{Model}'...", settings.Value.Ai.ModelFile);
                                TransitionTo(VacanamState.Processing);
                                overlayViewModel.State = VacanamState.Processing;
                                overlayViewModel.StatusLabel = "AI mode…";

                                string refined = await textProcessor.ProcessAsync(transcript, _currentSessionContext);
                                if (!string.IsNullOrWhiteSpace(refined))
                                {
                                    logger.LogInformation(">>> REFINED TRANSCRIPT: '{Refined}' <<<", refined);
                                    transcript = refined;
                                    wasActuallyEnhanced = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "AI text enhancement failed. Falling back to raw transcript.");
                            }
                        }
                    }
                }

                overlayViewModel.StatusLabel = transcript;

                // Text Injection into target window
                TransitionTo(VacanamState.Inserting);
                overlayViewModel.State = VacanamState.Inserting;

                await textInjector.InjectAsync(transcript, _currentSessionContext);
                audioFeedbackService.Play(AudioCue.Success);

                if (_isAiTransformMode)
                {
                    // In Ask AI / Voice Transform mode, always preserve the AI response on the Windows clipboard.
                    // If the user selected text from a read-only source (e.g. Outlook reading pane, PDF, webpage),
                    // the Ctrl+V attempt above cannot modify the read-only view. Leaving the response on the clipboard
                    // allows the user to simply click "Reply" (or open any editor) and press Ctrl+V to paste.
                    try
                    {
                        await clipboardService.SetTextAsync(transcript);
                        overlayViewModel.StatusLabel = "📋 Copied to Clipboard (Ctrl+V to paste)";
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to copy transformed text to clipboard.");
                    }
                }

                if (settings.Value.Privacy.SaveHistory)
                {
                    try
                    {
                        var record = new TranscriptRecord(
                            Id: 0,
                            TimestampUtc: DateTime.UtcNow,
                            RawTranscript: rawTranscript,
                            FinalText: transcript,
                            TargetApp: string.IsNullOrWhiteSpace(_currentSessionContext.ProcessName) ? "Unknown" : _currentSessionContext.ProcessName,
                            DurationSeconds: duration.TotalSeconds,
                            WasAiEnhanced: wasActuallyEnhanced
                        );
                        await historyRepository.AddAsync(record);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to save transcript to local history database.");
                    }
                }

                TransitionTo(VacanamState.Completed);
                overlayViewModel.State = VacanamState.Completed;
                await Task.Delay(_isAiTransformMode ? 1000 : 300);
            }

            TransitionTo(VacanamState.Idle);
            overlayViewModel.State = VacanamState.Idle;
            HideOverlay();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transcription & injection pipeline: {Message}", ex.Message);
            audioFeedbackService.Play(AudioCue.Error);
            overlayViewModel.StatusLabel = $"Error: {ex.Message}";
            TransitionTo(VacanamState.Error);
            await Task.Delay(2500);
            TransitionTo(VacanamState.Idle);
            overlayViewModel.State = VacanamState.Idle;
            HideOverlay();
        }
        finally
        {
            _isAiTransformMode = false;
            _capturedSelectedText = null;
            _recordingBuffer?.Dispose();
            _recordingBuffer = null;
        }
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

    private void OnQuickStartRequested(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var banner = new LaunchBannerWindow(settings.Value, modelManager, settingsManager);
            banner.SettingsRequested += OnSettingsRequested;
            banner.Show();
            banner.Activate();
        });
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            settingsViewModel.Reload();
            var settingsWindow = new SettingsWindow(settingsViewModel);
            settingsWindow.Show();
            settingsWindow.Activate();
        });
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        logger.LogInformation("Exit requested — shutting down.");
        // Stop the Generic Host first (triggers StopAsync + Cleanup)
        lifetime.StopApplication();
        // Force WPF dispatcher shutdown so the process actually exits
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Application.Current.Shutdown();
        });
    }

    private void Cleanup()
    {
        _audioMeterTimer?.Stop();
        _audioMeterTimer = null;

        mainViewModel.QuickStartRequested -= OnQuickStartRequested;
        mainViewModel.SettingsRequested -= OnSettingsRequested;

        hotkeyService.HotkeyPressed  -= OnHotkeyPressed;
        hotkeyService.HotkeyReleased -= OnHotkeyReleased;
        hotkeyService.AiTransformHotkeyPressed  -= OnAiTransformHotkeyPressed;
        hotkeyService.AiTransformHotkeyReleased -= OnAiTransformHotkeyReleased;
        hotkeyService.Unregister();

        _recordingBuffer?.Dispose();
        _recordingBuffer = null;

        audioRecorder.Dispose();
        speechRecognizer.Dispose();
        textProcessor.Dispose();

        _trayIcon?.Dispose();
        _trayIcon = null;

        logger.LogDebug("ApplicationLifetimeService cleaned up.");
    }

    public void Dispose() => Application.Current.Dispatcher.Invoke(Cleanup);
}
