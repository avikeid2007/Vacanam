using Vacanam.Core.Models;

namespace Vacanam.Core.Interfaces;

/// <summary>
/// Persists completed recording sessions to local SQLite storage.
/// Only active when the user has opted in via Settings ? Privacy.
/// NEVER stores raw audio data.
/// </summary>
public interface IHistoryRepository
{
    /// <summary>Saves a completed session record. No-op if history is disabled.</summary>
    Task SaveAsync(RecordingSession session, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent sessions, newest first.</summary>
    Task<IReadOnlyList<RecordingSession>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);

    /// <summary>Deletes all stored history entries.</summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
