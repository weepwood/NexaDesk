using Windows.ApplicationModel;

namespace NexaDesk;

public sealed class StartupService
{
    private const string TaskId = "NexaDeskStartup";

    public async Task<(bool Supported, bool Enabled, string Message)> GetStateAsync()
    {
        try
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            bool enabled = task.State is
                StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            return (true, enabled, task.State.ToString());
        }
        catch
        {
            return (false, false, "便携版不支持 Windows 启动任务。");
        }
    }

    public async Task<(bool Success, bool Enabled, string Message)> SetEnabledAsync(bool enabled)
    {
        try
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            if (enabled)
            {
                StartupTaskState state = await task.RequestEnableAsync();
                bool accepted = state is
                    StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
                return (accepted, accepted, state.ToString());
            }

            task.Disable();
            return (true, false, "Disabled");
        }
        catch (Exception ex)
        {
            return (false, false, ex.Message);
        }
    }
}
