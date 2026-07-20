using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NexaDesk.Views;
using Windows.Graphics;

namespace NexaDesk;

public sealed partial class MainWindow : Window
{
    private GlobalHotkeyService? _hotkeyService;
    private TrayIconService? _trayIconService;
    private CommandPaletteWindow? _paletteWindow;
    private Button[] _navigationButtons = [];
    private bool _allowClose;
    private bool _startupCompleted;
    private bool _shellIntegrationsInitialized;

    public MainWindow()
    {
        InitializeComponent();

        _navigationButtons =
        [
            HomeNavigationButton,
            ActionsNavigationButton,
            WorkflowsNavigationButton,
            HistoryNavigationButton,
            SettingsNavigationButton
        ];

        Title = "NexaDesk";
        TryConfigureTitleBarAndBackdrop();

        AppWindow.Resize(new SizeInt32(1180, 760));
        AppWindow.Closing += OnAppWindowClosing;
        Closed += OnClosed;
    }

    public void SetStartupStatus(string message)
    {
        StartupMessage.Text = message;
    }

    public void CompleteStartup()
    {
        InitializeShellIntegrations();

        NavigationShell.IsHitTestVisible = true;
        StartupOverlay.Visibility = Visibility.Collapsed;
        SelectNavigationButton("home");
        Navigate("home");
        _startupCompleted = true;
    }

    public void ShowStartupFailure(Exception exception)
    {
        _startupCompleted = false;
        NavigationShell.IsHitTestVisible = false;
        StartupOverlay.Visibility = Visibility.Visible;
        StartupProgress.IsActive = false;
        StartupProgress.Visibility = Visibility.Collapsed;
        StartupTitle.Text = "NexaDesk 启动失败";
        StartupMessage.Text = string.IsNullOrWhiteSpace(exception.Message)
            ? "本地服务初始化失败。"
            : exception.Message;
        StartupDetails.Text = $"诊断日志：{AppPaths.LogPath}";
        StartupDetails.Visibility = Visibility.Visible;
        StartupActions.Visibility = Visibility.Visible;
        ShowMainWindow();
    }

    public void EnsureVisible()
    {
        try
        {
            DisplayArea area = DisplayArea.GetFromWindowId(
                AppWindow.Id,
                DisplayAreaFallback.Primary);

            RectInt32 work = area.WorkArea;
            int width = Math.Min(1180, Math.Max(480, work.Width - 48));
            int height = Math.Min(760, Math.Max(360, work.Height - 48));
            int x = work.X + Math.Max(0, (work.Width - width) / 2);
            int y = work.Y + Math.Max(0, (work.Height - height) / 2);

            AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
        catch (Exception exception)
        {
            App.LogDiagnostic("Unable to position the main window.", exception);
        }
    }

    public bool WriteStartupProbe()
    {
        try
        {
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            bool visible = hwnd != 0 && IsWindowVisible(hwnd);

            AppPaths.EnsureCreated();
            File.WriteAllLines(
                AppPaths.StartupProbePath,
                [
                    $"hwnd={hwnd}",
                    $"visible={visible.ToString().ToLowerInvariant()}",
                    $"timestamp={DateTimeOffset.UtcNow:O}"
                ]);

            return visible;
        }
        catch (Exception exception)
        {
            App.LogDiagnostic("Unable to write the startup visibility probe.", exception);
            return false;
        }
    }

    public void ShowMainWindow()
    {
        AppWindow.Show();
        Activate();
        EnsureVisible();
    }

    public void ShowCommandPalette()
    {
        if (!_startupCompleted)
        {
            ShowMainWindow();
            return;
        }

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

    private void TryConfigureTitleBarAndBackdrop()
    {
        try
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }
        catch (Exception exception)
        {
            App.LogDiagnostic("Custom title bar is unavailable.", exception);
        }

        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch (Exception exception)
        {
            App.LogDiagnostic("Mica backdrop is unavailable; using the default background.", exception);
        }
    }

    private void InitializeShellIntegrations()
    {
        if (_shellIntegrationsInitialized)
        {
            return;
        }

        _shellIntegrationsInitialized = true;
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        try
        {
            GlobalHotkeyService hotkey = new();
            hotkey.Initialize(hwnd);
            hotkey.Triggered += (_, _) => ShowCommandPalette();
            _hotkeyService = hotkey;
        }
        catch (Exception exception)
        {
            _hotkeyService?.Dispose();
            _hotkeyService = null;
            App.LogDiagnostic("Global hotkey registration failed.", exception);
        }

        try
        {
            TrayIconService tray = new(DispatcherQueue);
            tray.Initialize(hwnd);

            if (!tray.IsAvailable)
            {
                tray.Dispose();
                App.LogDiagnostic("System tray icon is unavailable.");
                return;
            }

            tray.ShowRequested += (_, _) => ShowMainWindow();
            tray.PaletteRequested += (_, _) => ShowCommandPalette();
            tray.ExitRequested += (_, _) => RequestExit();
            _trayIconService = tray;
        }
        catch (Exception exception)
        {
            _trayIconService?.Dispose();
            _trayIconService = null;
            App.LogDiagnostic("System tray initialization failed.", exception);
        }
    }

    private void OnNavigationButtonClicked(object sender, RoutedEventArgs e)
    {
        if (!_startupCompleted || sender is not Button { Tag: string tag })
        {
            return;
        }

        SelectNavigationButton(tag);
        Navigate(tag);
    }

    private void SelectNavigationButton(string selectedTag)
    {
        Brush? selectedBrush = null;
        try
        {
            selectedBrush = Application.Current.Resources["NexaNavSelectedBrush"] as Brush;
        }
        catch
        {
        }

        foreach (Button button in _navigationButtons)
        {
            bool selected = string.Equals(button.Tag as string, selectedTag, StringComparison.Ordinal);
            if (selected && selectedBrush is not null)
            {
                button.Background = selectedBrush;
            }
            else
            {
                button.ClearValue(Control.BackgroundProperty);
            }

            button.Opacity = selected ? 1.0 : 0.78;
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

        if (!_startupCompleted || _trayIconService is null)
        {
            _allowClose = true;
            return;
        }

        args.Cancel = true;
        AppWindow.Hide();
    }

    private void OnOpenLogFolderClicked(object sender, RoutedEventArgs e)
    {
        AppPaths.EnsureCreated();
        Process.Start(new ProcessStartInfo
        {
            FileName = AppPaths.LogDirectory,
            UseShellExecute = true
        });
    }

    private void OnExitClicked(object sender, RoutedEventArgs e) => RequestExit();

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _hotkeyService?.Dispose();
        _trayIconService?.Dispose();
        App.Services.Dispose();
        App.Current.Exit();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hwnd);
}