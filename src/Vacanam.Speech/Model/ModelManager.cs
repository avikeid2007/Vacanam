using Microsoft.Extensions.Logging;
using Whisper.net.Ggml;
using Vacanam.Core.Interfaces;

namespace Vacanam.Speech.Model;

/// <summary>
/// Manages Whisper and LLM model files stored on disk.
/// Paths resolve to %LOCALAPPDATA%\Vacanam\Models\
/// Includes download capability via WhisperGgmlDownloader.
/// </summary>
public sealed class ModelManager : IModelManager
{
    private readonly ILogger<ModelManager> _logger;

    public string ModelsRootPath { get; }
    public string WhisperModelsPath { get; }
    public string LlmModelsPath { get; }

    public ModelManager(ILogger<ModelManager> logger)
    {
        _logger = logger;

        ModelsRootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vacanam", "Models");

        WhisperModelsPath = Path.Combine(ModelsRootPath, "Whisper");
        LlmModelsPath = Path.Combine(ModelsRootPath, "LLM");

        Directory.CreateDirectory(WhisperModelsPath);
        Directory.CreateDirectory(LlmModelsPath);
    }

    public string GetWhisperModelPath(string modelSize)
    {
        string fileName = $"ggml-{modelSize.ToLowerInvariant()}.bin";
        return Path.Combine(WhisperModelsPath, fileName);
    }

    public string GetLlmModelPath(string modelFileName)
    {
        return Path.Combine(LlmModelsPath, modelFileName);
    }

    public bool WhisperModelExists(string modelSize)
    {
        string path = GetWhisperModelPath(modelSize);
        bool exists = File.Exists(path) && new FileInfo(path).Length > 1024;
        return exists;
    }

    public bool LlmModelExists(string modelFileName)
    {
        if (string.IsNullOrWhiteSpace(modelFileName)) return false;
        string path = GetLlmModelPath(modelFileName);
        return File.Exists(path) && new FileInfo(path).Length > 1024;
    }

    /// <summary>
    /// Downloads the specified Whisper model from Hugging Face / official repo
    /// if it does not already exist on disk.
    /// </summary>
    public async Task EnsureWhisperModelDownloadedAsync(
        string modelSize,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string targetPath = GetWhisperModelPath(modelSize);
        if (WhisperModelExists(modelSize))
        {
            _logger.LogInformation("Whisper model '{Size}' already exists at {Path}.", modelSize, targetPath);
            return;
        }

        _logger.LogInformation("Downloading Whisper model '{Size}' to {Path}...", modelSize, targetPath);

        var ggmlType = modelSize.ToLowerInvariant() switch
        {
            "tiny" => GgmlType.Tiny,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            "large-v3" or "large" => GgmlType.LargeV3,
            _ => GgmlType.Small
        };

        string tempPath = targetPath + ".tmp";
        try
        {
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType, cancellationToken: cancellationToken);
            using var fileStream = File.Create(tempPath);

            byte[] buffer = new byte[81920];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await modelStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;
                // Progress is approximate since content-length isn't directly exposed by stream
                progress?.Report(totalBytesRead / (1024.0 * 1024.0));
            }

            fileStream.Close();
            if (File.Exists(targetPath)) File.Delete(targetPath);
            File.Move(tempPath, targetPath);

            _logger.LogInformation("Whisper model '{Size}' download complete ({SizeMb:F1} MB).", modelSize, totalBytesRead / (1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Whisper model '{Size}'.", modelSize);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }
}
