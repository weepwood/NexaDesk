using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace NexaDesk;

internal static class Program
{
    private const uint MbOk = 0x00000000;
    private const uint MbIconError = 0x00000010;
    private static App? _app;

    [STAThread]
    private static int Main(string[] args)
    {
        BootstrapLog("Process entry reached.");
        StartWatchdog();

        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            BootstrapLog("COM wrappers initialized.");

            Application.Start(_ =>
            {
                try
                {
                    DispatcherQueue queue = DispatcherQueue.GetForCurrentThread()
                        ?? throw new InvalidOperationException("Unable to acquire the WinUI dispatcher queue.");
                    SynchronizationContext.SetSynchronizationContext(
                        new DispatcherQueueSynchronizationContext(queue));
                    BootstrapLog("WinUI dispatcher initialized.");

                    _app = new App();
                    BootstrapLog("App object constructed.");
                }
                catch (Exception exception)
                {
                    BootstrapLog("App construction failed.", exception);
                    ShowFatalError(
                        "NexaDesk 无法初始化 WinUI。",
                        exception);
                    Environment.Exit(1);
                }
            });

            BootstrapLog("Application.Start returned.");
            return 0;
        }
        catch (Exception exception)
        {
            BootstrapLog("Process bootstrap failed.", exception);
            ShowFatalError("NexaDesk 启动引导失败。", exception);
            return 1;
        }
    }

    private static void StartWatchdog()
    {
        Thread watchdog = new(() =>
        {
            try
            {
                Thread.Sleep(TimeSpan.FromSeconds(12));
                string probe = AppPaths.StartupProbePath;
                if (File.Exists(probe) &&
                    File.ReadAllText(probe).Contains("visible=true", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                BootstrapLog("Startup watchdog did not observe a visible window.");
                MessageBox(
                    0,
                    "NexaDesk 已启动，但 WinUI 主窗口未能在 12 秒内显示。\n\n" +
                    "请查看以下日志：\n" +
                    AppPaths.BootstrapLogPath + "\n" +
                    AppPaths.LogPath,
                    "NexaDesk 启动异常",
                    MbOk | MbIconError);
            }
            catch (Exception exception)
            {
                BootstrapLog("Startup watchdog failed.", exception);
            }
        })
        {
            IsBackground = true,
            Name = "NexaDesk.StartupWatchdog"
        };
        watchdog.Start();
    }

    internal static void BootstrapLog(string message, Exception? exception = null)
    {
        try
        {
            AppPaths.EnsureCreated();
            string details = exception is null ? string.Empty : $"{Environment.NewLine}{exception}";
            File.AppendAllText(
                AppPaths.BootstrapLogPath,
                $"[{DateTimeOffset.Now:O}] {message}{details}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static void ShowFatalError(string title, Exception exception)
    {
        MessageBox(
            0,
            title + "\n\n" + exception.Message + "\n\n日志：\n" + AppPaths.BootstrapLogPath,
            "NexaDesk 启动失败",
            MbOk | MbIconError);
    }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(nint windowHandle, string text, string caption, uint type);
}
