using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using NexaDesk.Models;
using Windows.ApplicationModel;

namespace NexaDesk;

public sealed class UpdateService
{
    private static readonly Uri ReleasePage =
        new("https://github.com/weepwood/NexaDesk/releases/latest");

    public bool IsPackaged
    {
        get
        {
            try
            {
                _ = Package.Current.Id.Name;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsPackaged)
        {
            try
            {
                PackageUpdateAvailabilityResult result =
                    await Package.Current.CheckUpdateAvailabilityAsync();

                return result.Availability switch
                {
                    PackageUpdateAvailability.Available =>
                        new(true, false, "检测到新版本，Windows 将按更新策略安装。"),
                    PackageUpdateAvailability.Required =>
                        new(true, true, "此更新为必需更新。"),
                    PackageUpdateAvailability.NoUpdates =>
                        new(false, false, "当前已是最新版本。"),
                    PackageUpdateAvailability.Error =>
                        new(false, false, $"检查失败：{result.ExtendedError?.Message}"),
                    _ =>
                        new(false, false, "当前安装没有关联 App Installer 更新源。")
                };
            }
            catch (Exception ex)
            {
                return new(false, false, $"MSIX 更新检查失败：{ex.Message}");
            }
        }

        return await CheckPortableReleaseAsync(cancellationToken);
    }

    public static void OpenLatestRelease()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = ReleasePage.ToString(),
            UseShellExecute = true
        });
    }

    private static async Task<UpdateCheckResult> CheckPortableReleaseAsync(
        CancellationToken cancellationToken)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("NexaDesk", GetCurrentVersion().ToString()));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using HttpResponseMessage response = await client.GetAsync(
            "https://api.github.com/repos/weepwood/NexaDesk/releases/latest",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new(false, false, $"GitHub 返回 {(int)response.StatusCode}。");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        string tag = document.RootElement.GetProperty("tag_name").GetString() ?? "v0.0.0";
        string latestText = tag.TrimStart('v', 'V');
        Version current = GetCurrentVersion();

        if (!Version.TryParse(latestText, out Version? latest))
        {
            return new(false, false, $"无法解析发布版本：{tag}");
        }

        bool available = latest > current;
        return new(
            available,
            false,
            available
                ? $"发现新版本 {latest}。便携版需要手动下载安装。"
                : "当前已是最新版本。",
            latest.ToString(),
            ReleasePage);
    }

    private static Version GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version ??
        new Version(0, 0, 0, 0);
}
