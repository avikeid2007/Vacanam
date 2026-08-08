using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Models;

namespace Vacanam.Infrastructure.Configuration;

/// <summary>
/// Loads and persists Vacanam settings to/from a JSON file in %LOCALAPPDATA%\Vacanam\.
/// Thread-safe for concurrent reads; uses a lock for writes.
/// </summary>
public sealed class SettingsManager(ILogger<SettingsManager> logger)
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vacanam");

    private static readonly string SettingsFilePath =
        Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Lock _writeLock = new();

    /// <summary>
    /// Loads settings from disk. Returns default settings if the file does not exist.
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            logger.LogInformation("Settings file not found at {Path}. Using defaults.", SettingsFilePath);
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            logger.LogInformation("Settings loaded from {Path}.", SettingsFilePath);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load settings from {Path}. Using defaults.", SettingsFilePath);
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves the current settings to disk. Creates the directory if necessary.
    /// </summary>
    public void Save(AppSettings settings)
    {
        lock (_writeLock)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                logger.LogInformation("Settings saved to {Path}.", SettingsFilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save settings to {Path}.", SettingsFilePath);
            }
        }
    }
}
