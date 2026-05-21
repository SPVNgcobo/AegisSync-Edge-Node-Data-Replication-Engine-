namespace AegisSync.EdgeNode.Workers;

using AegisSync.EdgeNode.FSM;

public class FsmWorker : BackgroundService
{
    private readonly SyncContext _syncContext;
    private readonly ILogger<FsmWorker> _logger;

    public FsmWorker(SyncContext syncContext, ILogger<FsmWorker> logger)
    {
        _syncContext = syncContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("State Machine Worker started.");

        // The Heartbeat Loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // The current state dictates what happens when ExecuteAsync is called
                await _syncContext.CurrentState.ExecuteAsync(_syncContext, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                // Unhandled exception fallback to prevent the worker from crashing completely
                _logger.LogCritical(ex, "Fatal error in State Machine. Forcing Offline State.");
                _syncContext.TransitionTo(new OfflineState());
                await Task.Delay(5000, stoppingToken); 
            }
        }
    }
} 
