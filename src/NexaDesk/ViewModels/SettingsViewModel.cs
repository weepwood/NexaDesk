using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NexaDesk.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool startupSupported;

    [ObservableProperty]
    private bool startupEnabled;

    [ObservableProperty]
    private bool packaged;

    [ObservableProperty]
    private string theme = "System";

    [ObservableProperty]
    private string statusMessage = "设置仅保存在本机。";

    public async Task LoadAsync()
    {
        Packaged = App.Services.Updates.IsPackaged;
        Theme = App.Services.Settings.GetCached("theme", "System");

        (bool supported, bool enabled, string message) =
            await App.Services.Startup.GetStateAsync();
        StartupSupported = supported;
        StartupEnabled = enabled;

        if (!supported)
        {
            StatusMessage = message;
        }
    }

    public async Task SetStartupAsync(bool enabled)
    {
        (bool success, bool actualEnabled, string message) =
            await App.Services.Startup.SetEnabledAsync(enabled);

        StartupEnabled = actualEnabled;
        StatusMessage = success
            ? (actualEnabled ? "已启用开机启动。" : "已关闭开机启动。")
            : $"无法修改开机启动：{message}";
    }

    public async Task SetThemeAsync(string value)
    {
        Theme = value;
        await App.Services.Settings.SetAsync("theme", value);
        App.ApplyTheme(value);
        StatusMessage = "主题已保存。";
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        StatusMessage = "正在检查更新…";
        Models.UpdateCheckResult result = await App.Services.Updates.CheckAsync();
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private void OpenReleasePage() => UpdateService.OpenLatestRelease();

    [RelayCommand]
    private void OpenDataFolder()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = AppPaths.RootDirectory,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task RescanApplicationsAsync()
    {
        StatusMessage = "正在扫描开始菜单…";
        int count = await App.Services.ApplicationIndex.RefreshAsync();
        StatusMessage = $"应用索引已更新，共发现 {count} 个入口。";
    }
}
