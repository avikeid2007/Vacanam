namespace Vacanam.Core.Interfaces;

/// <summary>
/// Provides safe, async-compatible clipboard operations with backup/restore support.
/// Used by the clipboard-based text injection strategy.
/// </summary>
public interface IClipboardService
{
    /// <summary>Gets the current clipboard text. Returns null if clipboard contains non-text data.</summary>
    Task<string?> GetTextAsync();

    /// <summary>Sets the clipboard to the given text.</summary>
    Task SetTextAsync(string text);

    /// <summary>
    /// Saves the current clipboard state, allowing it to be restored later.
    /// Returns an opaque token identifying the saved state.
    /// </summary>
    Task<object?> BackupAsync();

    /// <summary>Restores a previously saved clipboard state.</summary>
    Task RestoreAsync(object? backup);
}
