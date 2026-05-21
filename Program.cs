// FSM Registration
builder.Services.AddSingleton<SyncContext>();

// Register the mock interfaces (You will build the real implementations later)
builder.Services.AddSingleton<ISqliteRepository, SqliteRepository>();
builder.Services.AddSingleton<IGrpcSyncClient, GrpcSyncClient>();

// Start the FSM Engine
builder.Services.AddHostedService<FsmWorker>();

// Register the actual repository instead of the mock
builder.Services.AddSingleton<ISqliteRepository, SqliteRepository>();

var host = builder.Build();

// Initialize the database before starting the workers
using (var scope = host.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ISqliteRepository>();
    await repo.InitializeDatabaseAsync();
}

host.Run();
