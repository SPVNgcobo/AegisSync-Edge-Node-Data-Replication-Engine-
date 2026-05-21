using AegisSync.EdgeNode.Models;

namespace AegisSync.EdgeNode.Infrastructure;

public interface ISqliteRepository
{
    Task InitializeDatabaseAsync();
    Task SavePendingEventAsync(SyncEvent syncEvent, CancellationToken token);
    Task<int> GetPendingCountAsync(CancellationToken token);
    Task<IEnumerable<SyncEvent>> GetPendingBatchAsync(int batchSize, CancellationToken token);
    Task MarkAsSyncedAsync(IEnumerable<Guid> eventIds, CancellationToken token);
}
