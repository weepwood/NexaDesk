namespace NexaDesk;

public sealed class AppServices : IDisposable
{
    public DatabaseService Database { get; }
    public SettingsService Settings { get; }
    public WindowService Windows { get; }
    public ActionExecutionService Actions { get; }
    public WorkflowService Workflows { get; }
    public ApplicationIndexService ApplicationIndex { get; }
    public StartupService Startup { get; }
    public UpdateService Updates { get; }

    public AppServices()
    {
        Database = new DatabaseService();
        Settings = new SettingsService(Database);
        Windows = new WindowService();
        Actions = new ActionExecutionService(Database, Windows);
        Workflows = new WorkflowService(Database, Actions);
        ApplicationIndex = new ApplicationIndexService(Database);
        Startup = new StartupService();
        Updates = new UpdateService();
    }

    public async Task InitializeAsync()
    {
        AppPaths.EnsureCreated();
        await Database.InitializeAsync();
        await Settings.LoadCacheAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                await ApplicationIndex.RefreshIfStaleAsync();
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(
                    AppPaths.LogPath,
                    $"[{DateTimeOffset.Now:O}] Application index failed: {ex}\r\n");
            }
        });
    }

    public void Dispose() => Database.Dispose();
}
