using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed local repository for transcript history and fast full-text search.
/// Data is stored in %LOCALAPPDATA%\Vacanam\history.db using WAL mode.
/// </summary>
public sealed class SqliteTranscriptHistoryRepository : ITranscriptHistoryRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteTranscriptHistoryRepository> _logger;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SqliteTranscriptHistoryRepository(ILogger<SqliteTranscriptHistoryRepository> logger)
    {
        _logger = logger;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbDir = Path.Combine(appData, "Vacanam");
        Directory.CreateDirectory(dbDir);

        string dbPath = Path.Combine(dbDir, "history.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Enable WAL mode for low-latency concurrent reads/writes
            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA journal_mode=WAL;";
            await pragmaCmd.ExecuteNonQueryAsync(cancellationToken);

            // Create table & indices
            using var tableCmd = connection.CreateCommand();
            tableCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS TranscriptHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TimestampUtc TEXT NOT NULL,
                    RawTranscript TEXT NOT NULL,
                    FinalText TEXT NOT NULL,
                    TargetApp TEXT NOT NULL,
                    DurationSeconds REAL NOT NULL,
                    WasAiEnhanced INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_TranscriptHistory_Timestamp ON TranscriptHistory(TimestampUtc DESC);
                """;
            await tableCmd.ExecuteNonQueryAsync(cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("SQLite transcript history database initialized cleanly.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite history database.");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task AddAsync(TranscriptRecord record, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO TranscriptHistory (TimestampUtc, RawTranscript, FinalText, TargetApp, DurationSeconds, WasAiEnhanced)
                VALUES (@TimestampUtc, @RawTranscript, @FinalText, @TargetApp, @DurationSeconds, @WasAiEnhanced);
                """;
            command.Parameters.AddWithValue("@TimestampUtc", record.TimestampUtc.ToString("o"));
            command.Parameters.AddWithValue("@RawTranscript", record.RawTranscript ?? string.Empty);
            command.Parameters.AddWithValue("@FinalText", record.FinalText ?? string.Empty);
            command.Parameters.AddWithValue("@TargetApp", record.TargetApp ?? string.Empty);
            command.Parameters.AddWithValue("@DurationSeconds", record.DurationSeconds);
            command.Parameters.AddWithValue("@WasAiEnhanced", record.WasAiEnhanced ? 1 : 0);

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Saved transcript history record to SQLite.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert transcript record into SQLite history.");
        }
    }

    public async Task<IReadOnlyList<TranscriptRecord>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var list = new List<TranscriptRecord>();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Id, TimestampUtc, RawTranscript, FinalText, TargetApp, DurationSeconds, WasAiEnhanced
                FROM TranscriptHistory
                ORDER BY Id DESC
                LIMIT @Limit;
                """;
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(ReadRecord(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query recent transcript history from SQLite.");
        }

        return list;
    }

    public async Task<IReadOnlyList<TranscriptRecord>> SearchAsync(string query, int limit = 100, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetRecentAsync(limit, cancellationToken);
        }

        var list = new List<TranscriptRecord>();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Id, TimestampUtc, RawTranscript, FinalText, TargetApp, DurationSeconds, WasAiEnhanced
                FROM TranscriptHistory
                WHERE FinalText LIKE @Query OR RawTranscript LIKE @Query OR TargetApp LIKE @Query
                ORDER BY Id DESC
                LIMIT @Limit;
                """;
            command.Parameters.AddWithValue("@Query", $"%{query.Trim()}%");
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(ReadRecord(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search transcript history in SQLite.");
        }

        return list;
    }

    public async Task DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TranscriptHistory WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete transcript record {Id} from SQLite.", id);
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM TranscriptHistory;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("All transcript history cleared from SQLite.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear transcript history in SQLite.");
        }
    }

    private static TranscriptRecord ReadRecord(SqliteDataReader reader)
    {
        long id = reader.GetInt64(0);
        string tsStr = reader.GetString(1);
        DateTime timestampUtc = DateTime.TryParse(tsStr, out var dt) ? dt : DateTime.UtcNow;
        string raw = reader.GetString(2);
        string final = reader.GetString(3);
        string targetApp = reader.GetString(4);
        double duration = reader.GetDouble(5);
        bool wasAi = reader.GetInt32(6) != 0;

        return new TranscriptRecord(id, timestampUtc, raw, final, targetApp, duration, wasAi);
    }
}
