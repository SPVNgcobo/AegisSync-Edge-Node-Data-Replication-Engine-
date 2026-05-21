namespace AegisSync.EdgeNode.FSM;

// 1. The Contract: Every state must know how to execute its behavior
public interface INetworkState
{
    Task ExecuteAsync(SyncContext context, CancellationToken token);
}

// 2. The Context: Holds dependencies and tracks the current state
public class SyncContext
{
    public INetworkState CurrentState { get; private set; }
    public ILogger<SyncContext> Logger { get; }
    
    // Dependencies (Mocked as interfaces for now)
    public ISqliteRepository LocalDb { get; }
    public IGrpcSyncClient SyncClient { get; }

    public SyncContext(
        ILogger<SyncContext> logger, 
        ISqliteRepository localDb, 
        IGrpcSyncClient syncClient)
    {
        Logger = logger;
        LocalDb = localDb;
        SyncClient = syncClient;
        
        // Always start under the assumption we are offline for safety
        CurrentState = new OfflineState(); 
    }

    public void TransitionTo(INetworkState newState)
    {
        Logger.LogInformation("FSM Transition: {OldState} -> {NewState}", 
            CurrentState.GetType().Name, newState.GetType().Name);
            
        CurrentState = newState;
    }
}
