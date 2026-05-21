using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using AegisSync.EdgeNode.Models;

namespace AegisSync.EdgeNode.Infrastructure;

public class SqliteRepository : ISqliteRepository
{
    // In production, this comes from appsettings.json
    private const string ConnectionString = "Data Source=aegis_events.db;";
    private readonly ILogger<SqliteRepository> _logger;

    public SqliteRepository(ILogger<SqliteRepository> logger)
    {
        _logger = logger;
    }

    private SqliteConnection CreateConnection() => new SqliteConnection(ConnectionString);

    public async Task InitializeDatabaseAsync()
    {
        using var connection = CreateConnection();
        
        // Ensure the table exists. We use an index on Status for fast querying by the FSM.
        var tableCmd = @"
            CREATE TABLE IF NOT EXISTS SyncEvents (
                Id TEXT PRIMARY KEY,
                EventType TEXT NOT NULL,
                JsonPayload TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                Status TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_status ON SyncEvents(Status);";

        await connection.ExecuteAsync(tableCmd);
        _logger.LogInformation("SQLite database initialized successfully.");
    }

    public async Task SavePendingEventAsync(SyncEvent syncEvent, CancellationToken token)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO SyncEvents (Id, EventType, JsonPayload, CreatedAtUtc, Status)
            VALUES (@Id, @EventType, @JsonPayload, @CreatedAtUtc, 'Pending')";

        await connection.ExecuteAsync(sql, syncEvent);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken token)
    {
        using var connection = CreateConnection();
        var sql = "SELECT COUNT(1) FROM SyncEvents WHERE Status = 'Pending'";
        
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task<IEnumerable<SyncEvent>> GetPendingBatchAsync(int batchSize, CancellationToken token)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT Id, EventType, JsonPayload, CreatedAtUtc 
            FROM SyncEvents 
            WHERE Status = 'Pending' 
            ORDER BY CreatedAtUtc ASC 
            LIMIT @BatchSize";

        return await connection.QueryAsync<SyncEvent>(sql, new { BatchSize = batchSize });
    }

    public async Task MarkAsSyncedAsync(IEnumerable<Guid> eventIds, CancellationToken token)
    {
        using var connection = CreateConnection();
        
        // Dapper safely handles the IN clause expansion
        var sql = "UPDATE SyncEvents SET Status = 'Synced' WHERE Id IN @Ids";
        
        await connection.ExecuteAsync(sql, new { Ids = eventIds.Select(g => g.ToString()) });
    }
}
