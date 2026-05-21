// FSM Registration
builder.Services.AddSingleton<SyncContext>();

// Register the mock interfaces (You will build the real implementations later)
builder.Services.AddSingleton<ISqliteRepository, SqliteRepository>();
builder.Services.AddSingleton<IGrpcSyncClient, GrpcSyncClient>();

// Start the FSM Engine
builder.Services.AddHostedService<FsmWorker>();
