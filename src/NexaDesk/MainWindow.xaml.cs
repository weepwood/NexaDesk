using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NexaDesk.Views;

namespace NexaDesk;

public sealed partial class MainWindow : Window
{
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly TrayIconService _trayIconService;
    private CommandPaletteWindow? _paletteWindow;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        Title = "NexaDesk";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 760));
        AppWindow.Closing += OnAppWindowClosing;
        Closed += OnClosed;

        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        _hotkeyService = new GlobalHotkeyService();
        try
        {
            _hotkeyService.Initialize(hwnd);
            _hotkeyService.Triggered += (_, _) => ShowCommandPalette();
        }
        catch (Exception exception)
        {
            LogStartupWarning("Global hotkey registration failed.", exception);
        }

        _trayIconService = new TrayIconService(DispatcherQueue);
        _trayIconService.Initialize(hwnd);
        _trayIconService.ShowRequested += (_, _) => ShowMainWindow();
        _trayIconService.PaletteRequested += (_, _) => ShowCommandPalette();
        _trayIconService.ExitRequested += (_, _) => RequestExit();

        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
        Navigate("home");
    }

    public void ShowMainWindow()
    {
        AppWindow.Show();
        Activate();
    }

    public void ShowCommandPalette()
    {
        App.Services.Windows.CaptureForegroundWindow();
        _paletteWindow ??= new CommandPaletteWindow();
        _paletteWindow.ShowPalette();
    }

    public void RequestExit()
    {
        _allowClose = true;
        _paletteWindow?.Close();
        Close();
    }

    private void OnNavigationSelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag)
        {
            Navigate(tag);
        }
    }

    private void Navigate(string tag)
    {
        Type pageType = tag switch
        {
            "actions" => typeof(ActionsPage),
            "workflows" => typeof(WorkflowsPage),
            "history" => typeof(HistoryPage),
            "settings" => typeof(SettingsPage),
            _ => typeof(HomePage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        AppWindow.Hide();
    }

    private static void LogStartupWarning(string message, Exception exception)
    {
        try
        {
            AppPaths.EnsureCreated();
            File.AppendAllText(
                AppPaths.LogPath,
                $"[{DateTimeOffset.Now:O}] {message} {exception}\r\n");
        }
        catch
        {
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _hotkeyService.Dispose();
        _trayIconService.Dispose();
        App.Current.Exit();
    }
}
