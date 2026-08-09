using Vacanam.Core.Models;

namespace Vacanam.Core.Interfaces;

/// <summary>
/// Repository interface for local transcript history persistence and search.
/// </summary>
public interface ITranscriptHistoryRepository
{
    Task AddAsync(TranscriptRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranscriptRecord>> SearchAsync(string query, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranscriptRecord>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(long id, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
