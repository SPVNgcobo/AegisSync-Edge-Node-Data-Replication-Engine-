namespace AegisSync.EdgeNode.FSM;

public class OfflineState : INetworkState
{
    public async Task ExecuteAsync(SyncContext context, CancellationToken token)
    {
        context.Logger.LogDebug("Pinging central server...");

        bool isOnline = await context.SyncClient.CheckConnectivityAsync(token);

        if (isOnline)
        {
            // Network restored! Move to Idle to check for pending data.
            context.TransitionTo(new IdleState());
        }
        else
        {
            // Wait 5 seconds before checking again (Exponential backoff goes here in production)
            await Task.Delay(5000, token);
        }
    }
}

public class IdleState : INetworkState
{
    public async Task ExecuteAsync(SyncContext context, CancellationToken token)
    {
        // Check if we have anything to sync
        int pendingCount = await context.LocalDb.GetPendingCountAsync(token);

        if (pendingCount > 0)
        {
            context.Logger.LogInformation("Found {Count} pending events. Initiating sync.", pendingCount);
            context.TransitionTo(new SyncingState());
        }
        else
        {
            // Nothing to do. Rest for a bit.
            await Task.Delay(2000, token);
        }
    }
}

public class SyncingState : INetworkState
{
    public async Task ExecuteAsync(SyncContext context, CancellationToken token)
    {
        try
        {
            var batch = await context.LocalDb.GetPendingBatchAsync(100, token);
            
            // Push via gRPC
            bool success = await context.SyncClient.FlushBatchAsync(batch, token);

            if (success)
            {
                await context.LocalDb.MarkAsSyncedAsync(batch.Select(b => b.Id), token);
                // Batch successful, return to Idle to see if there is more data
                context.TransitionTo(new IdleState());
            }
            else
            {
                // Server rejected or failed, back to offline to protect the system
                context.TransitionTo(new OfflineState());
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "gRPC connection dropped during sync.");
            context.TransitionTo(new OfflineState());
        }
    }
}
