using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace NexaDesk;

public partial class App : Application
{
    private const uint MbOk = 0x00000000;
    private const uint MbIconError = 0x00000010;
    private static bool _startupCompleted;

    public static AppServices Services { get; } = new();
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppPaths.EnsureCreated();
        LogDiagnostic("NexaDesk startup started.");

        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            MainWindow.EnsureVisible();
        }
        catch (Exception exception)
        {
            LogDiagnostic("Main window creation failed.", exception);
            ShowFatalStartupError(exception);
            Exit();
            return;
        }

        try
        {
            MainWindow.SetStartupStatus("正在初始化本地数据库和设置…");
            await Services.InitializeAsync();

            ApplyTheme(Services.Settings.GetCached("theme", "System"));
            MainWindow.CompleteStartup();
            _startupCompleted = true;
            LogDiagnostic("NexaDesk startup completed.");
        }
        catch (Exception exception)
        {
            LogDiagnostic("Application service initialization failed.", exception);
            MainWindow.ShowStartupFailure(exception);
        }
    }

    public static void ApplyTheme(string theme)
    {
        if (MainWindow?.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    internal static void LogDiagnostic(string message, Exception? exception = null)
    {
        try
        {
            AppPaths.EnsureCreated();
            string details = exception is null ? string.Empty : $"{Environment.NewLine}{exception}";
            File.AppendAllText(
                AppPaths.LogPath,
                $"[{DateTimeOffset.Now:O}] {message}{details}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static void ShowFatalStartupError(Exception exception)
    {
        string message =
            "NexaDesk 无法创建主窗口。\n\n" +
            exception.Message +
            "\n\n诊断日志：\n" +
            AppPaths.LogPath;

        MessageBox(0, message, "NexaDesk 启动失败", MbOk | MbIconError);
    }

    private static void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogDiagnostic("Unhandled XAML exception.", e.Exception);

        if (!_startupCompleted && MainWindow is not null)
        {
            e.Handled = true;
            MainWindow.ShowStartupFailure(e.Exception);
        }
    }

    private static void OnDomainUnhandledException(
        object? sender,
        System.UnhandledExceptionEventArgs e)
    {
        LogDiagnostic(
            "Unhandled AppDomain exception.",
            e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        LogDiagnostic("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(
        nint windowHandle,
        string text,
        string caption,
        uint type);
}
