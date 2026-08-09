using Microsoft.Extensions.Logging;

namespace Vacanam.LLM.Model;

public sealed class LlmModelManager
{
    private readonly string _llmModelsDir;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmModelManager> _logger;

    public LlmModelManager(ILogger<LlmModelManager> logger)
    {
        _logger = logger;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _llmModelsDir = Path.Combine(appData, "Vacanam", "Models", "LLM");
        Directory.CreateDirectory(_llmModelsDir);
        _httpClient = new HttpClient();
    }

    public string ModelsDirectory => _llmModelsDir;

    public bool LlmModelExists(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        string path = GetLlmModelPath(fileName);
        return File.Exists(path) && new FileInfo(path).Length > 1024;
    }

    public string GetLlmModelPath(string fileName)
    {
        return Path.Combine(_llmModelsDir, fileName);
    }

    public async Task EnsureLlmModelDownloadedAsync(
        string fileName,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var descriptor = LlmModelDescriptor.Catalog.FirstOrDefault(m => string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (descriptor is null)
        {
            throw new FileNotFoundException($"Model '{fileName}' is not in the recognized Vacanam LLM catalog.");
        }

        string targetPath = GetLlmModelPath(fileName);
        if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 1024)
        {
            _logger.LogInformation("LLM Model file '{FileName}' already exists.", fileName);
            return;
        }

        string tempPath = targetPath + ".tmp";
        _logger.LogInformation("Downloading LLM Model '{FileName}' from '{Url}'...", fileName, descriptor.DownloadUrl);

        using var response = await _httpClient.GetAsync(descriptor.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        long totalBytes = response.Content.Headers.ContentLength ?? descriptor.FileSizeBytes;
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        byte[] buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                double downloadedMb = totalRead / (1024.0 * 1024.0);
                progress?.Report(downloadedMb);
            }
        }

        fileStream.Close();
        File.Move(tempPath, targetPath, overwrite: true);
        _logger.LogInformation("LLM Model '{FileName}' download completed.", fileName);
    }
}
