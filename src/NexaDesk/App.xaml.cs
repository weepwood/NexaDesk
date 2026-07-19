using Microsoft.UI.Xaml;

namespace NexaDesk;

public partial class App : Application
{
    public static AppServices Services { get; } = new();
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = LaunchAsync();
    }

    private static async Task LaunchAsync()
    {
        await Services.InitializeAsync();
        MainWindow = new MainWindow();
        ApplyTheme(Services.Settings.GetCached("theme", "System"));
        MainWindow.Activate();
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

    private static void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            AppPaths.EnsureCreated();
            File.AppendAllText(
                AppPaths.LogPath,
                $"[{DateTimeOffset.Now:O}] {e.Exception}\r\n");
        }
        catch
        {
        }
    }
}
