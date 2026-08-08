using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whisper.net;
using Vacanam.Core.Exceptions;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Speech.Model;

namespace Vacanam.Speech.Recognition;

/// <summary>
/// Production speech recognition implementation wrapping whisper.cpp via Whisper.net.
/// Optimized for ultra-low-latency local voice typing (<300ms response time).
/// </summary>
public sealed class WhisperSpeechRecognizer : ISpeechRecognizer
{
    private readonly IOptions<AppSettings> _settings;
    private readonly ModelManager _modelManager;
    private readonly ILogger<WhisperSpeechRecognizer> _logger;

    private WhisperFactory? _factory;
    private string? _currentLoadedModelSize;
    private readonly SemaphoreSlim _modelLock = new(1, 1);
    private bool _disposed;

    public event EventHandler<TranscriptSegmentEventArgs>? SegmentReceived;
    public bool IsReady => _factory is not null;

    public WhisperSpeechRecognizer(
        IOptions<AppSettings> settings,
        ModelManager modelManager,
        ILogger<WhisperSpeechRecognizer> logger)
    {
        _settings = settings;
        _modelManager = modelManager;
        _logger = logger;
    }

    public async Task LoadModelAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string targetSize = _settings.Value.Speech.ModelSize;

        await _modelLock.WaitAsync(cancellationToken);
        try
        {
            if (_factory is not null && _currentLoadedModelSize == targetSize)
            {
                return;
            }

            _logger.LogInformation("Loading Whisper model '{Size}'...", targetSize);

            if (!_modelManager.WhisperModelExists(targetSize))
            {
                _logger.LogInformation("Model file missing for '{Size}'. Starting automatic download...", targetSize);
                await _modelManager.EnsureWhisperModelDownloadedAsync(targetSize, cancellationToken: cancellationToken);
            }

            string modelPath = _modelManager.GetWhisperModelPath(targetSize);
            if (!File.Exists(modelPath))
            {
                throw new ModelNotFoundException(modelPath);
            }

            _factory?.Dispose();
            _factory = null;

            _factory = WhisperFactory.FromPath(modelPath);
            _currentLoadedModelSize = targetSize;

            _logger.LogInformation("Whisper model '{Size}' successfully loaded into memory.", targetSize);
        }
        catch (Exception ex) when (ex is not ModelNotFoundException)
        {
            _logger.LogError(ex, "Failed to load Whisper model '{Size}'.", targetSize);
            throw new InferenceException($"Failed to load Whisper model '{targetSize}'.", ex);
        }
        finally
        {
            _modelLock.Release();
        }
    }

    public async Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (audioStream is null || audioStream.Length == 0)
        {
            _logger.LogWarning("TranscribeAsync received empty audio stream.");
            return string.Empty;
        }

        if (!IsReady || _currentLoadedModelSize != _settings.Value.Speech.ModelSize)
        {
            await LoadModelAsync(cancellationToken);
        }

        _logger.LogInformation("Starting Whisper transcription on {Bytes} bytes audio stream.", audioStream.Length);
        var sb = new StringBuilder();

        try
        {
            string language = _settings.Value.Speech.Language;
            if (string.IsNullOrWhiteSpace(language) || string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
            {
                language = "en"; // Default to English for low-latency; bypass language detection pass
            }

            // Calculate optimal CPU thread count for parallel inference
            int threads = Math.Max(2, Environment.ProcessorCount - 1);

            var builder = _factory!.CreateBuilder()
                .WithLanguage(language)
                .WithThreads(threads)
                .WithSingleSegment(); // Optimize for short voice typing clips

            using var processor = builder.Build();

            await foreach (var segment in processor.ProcessAsync(audioStream, cancellationToken))
            {
                string text = segment.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    sb.Append(text).Append(' ');
                    _logger.LogDebug("Whisper Segment: {Text} (t: {Start} -> {End})", text, segment.Start, segment.End);
                    SegmentReceived?.Invoke(this, new TranscriptSegmentEventArgs(text, isFinal: false));
                }
            }

            string fullTranscript = sb.ToString().Trim();
            _logger.LogInformation("Whisper transcription completed in low-latency mode ({Len} chars): '{Text}'", fullTranscript.Length, fullTranscript);
            return fullTranscript;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Transcription cancelled by caller.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Whisper transcription.");
            throw new InferenceException("Speech recognition failed.", ex);
        }
    }

    public async Task UnloadModelAsync()
    {
        await _modelLock.WaitAsync();
        try
        {
            _factory?.Dispose();
            _factory = null;
            _currentLoadedModelSize = null;
            _logger.LogInformation("Whisper model unloaded.");
        }
        finally
        {
            _modelLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _factory?.Dispose();
        _factory = null;
        _modelLock.Dispose();
        _logger.LogDebug("WhisperSpeechRecognizer disposed.");
    }
}
