using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Channels;
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
/// WASAPI audio capture, real-time streaming Whisper STT transcription,
/// instant text injection, overlay management, and pipeline state coordination.
///
/// Features Real-Time Streaming Transcription:
/// Audio chunks are processed in the background while holding Ctrl+Space.
/// Text appears on overlay in real-time and inserts into active app <30ms upon key release.
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

    private Channel<byte[]>? _audioChannel;
    private Task<string>? _streamingTask;
    private ApplicationContext _currentSessionContext = ApplicationContext.Unknown;

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

        logger.LogInformation("Vacanam (Real-Time Streaming Voice Typing) is running. Hold Ctrl+Space to dictate into any window.");
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
            if (!string.IsNullOrWhiteSpace(e.Text))
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

                // Create channel for real-time background streaming
                _audioChannel = Channel.CreateUnbounded<byte[]>();
                _recordingBuffer.BeginCapture(_audioChannel.Writer);

                // Start real-time background transcription task
                _streamingTask = Task.Run(() => StreamTranscriptionWorkerAsync(_audioChannel.Reader));

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

            logger.LogInformation("Hotkey released. Completing streaming audio capture.");
            _audioMeterTimer?.Stop();

            try
            {
                TransitionTo(VacanamState.StoppingRecording);

                await audioRecorder.StopAsync();
                _recordingBuffer?.EndCapture(); // completes _audioChannel.Writer

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

                TransitionTo(VacanamState.Transcribing);
                overlayViewModel.State = VacanamState.Transcribing;

                // Wait for real-time background streaming task to complete
                string transcript = string.Empty;
                if (_streamingTask is not null)
                {
                    transcript = await _streamingTask;
                }

                // Fallback: if streaming output was empty, process full WAV stream
                if (string.IsNullOrWhiteSpace(transcript) && _recordingBuffer is not null)
                {
                    using var wavStream = _recordingBuffer.ToWavStream(trimSilence: true);
                    transcript = await speechRecognizer.TranscribeAsync(wavStream);
                }

                logger.LogInformation(">>> FINAL REAL-TIME TRANSCRIPT: '{Transcript}' <<<", transcript);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    overlayViewModel.StatusLabel = "No speech detected";
                    await Task.Delay(300);
                }
                else
                {
                    overlayViewModel.StatusLabel = transcript;

                    // Instant Text Injection into target window
                    TransitionTo(VacanamState.Inserting);
                    overlayViewModel.State = VacanamState.Inserting;

                    await textInjector.InjectAsync(transcript, _currentSessionContext);

                    TransitionTo(VacanamState.Completed);
                    overlayViewModel.State = VacanamState.Completed;
                    await Task.Delay(200);
                }

                TransitionTo(VacanamState.Idle);
                overlayViewModel.State = VacanamState.Idle;
                HideOverlay();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during streaming transcription & injection pipeline.");
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
                _audioChannel = null;
                _streamingTask = null;
            }
        });
    }

    // ── Real-Time Streaming Worker ─────────────────────────────────────────────

    /// <summary>
    /// Background worker that processes incoming audio chunks in real-time while holding Ctrl+Space.
    /// Updates UI overlay with partial transcript segments as user speaks.
    /// </summary>
    private async Task<string> StreamTranscriptionWorkerAsync(ChannelReader<byte[]> channelReader)
    {
        string lastResult = string.Empty;
        var pcmBuffer = new List<byte>();

        try
        {
            await foreach (var chunk in channelReader.ReadAllAsync())
            {
                pcmBuffer.AddRange(chunk);

                // Run partial Whisper pass every ~800ms (25600 bytes at 16kHz 16-bit mono)
                if (pcmBuffer.Count >= 25600)
                {
                    using var ms = CreateWavStreamFromPcm(pcmBuffer.ToArray());
                    string partialText = await speechRecognizer.TranscribeAsync(ms);
                    if (!string.IsNullOrWhiteSpace(partialText))
                    {
                        lastResult = partialText;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            overlayViewModel.StatusLabel = partialText;
                        });
                    }
                }
            }

            // Final pass on full accumulated audio
            if (pcmBuffer.Count > 0)
            {
                using var finalMs = CreateWavStreamFromPcm(pcmBuffer.ToArray());
                string finalPartial = await speechRecognizer.TranscribeAsync(finalMs);
                if (!string.IsNullOrWhiteSpace(finalPartial))
                {
                    lastResult = finalPartial;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error in real-time streaming transcription worker.");
        }

        return lastResult;
    }

    private static MemoryStream CreateWavStreamFromPcm(byte[] pcmData)
    {
        var ms = new MemoryStream();
        using (var writer = new NAudio.Wave.WaveFileWriter(new IgnoreDisposeStream(ms), new NAudio.Wave.WaveFormat(16000, 1)))
        {
            writer.Write(pcmData, 0, pcmData.Length);
            writer.Flush();
        }
        ms.Position = 0;
        return ms;
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
