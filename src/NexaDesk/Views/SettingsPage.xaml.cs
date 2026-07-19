using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NexaDesk.ViewModels;

namespace NexaDesk.Views;

public sealed partial class SettingsPage : Page
{
    private bool _loading;
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loading = true;
        await ViewModel.LoadAsync();

        StartupToggle.IsOn = ViewModel.StartupEnabled;
        ThemeBox.SelectedIndex = ViewModel.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        _loading = false;
    }

    private async void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        await ViewModel.SetStartupAsync(StartupToggle.IsOn);
        _loading = true;
        StartupToggle.IsOn = ViewModel.StartupEnabled;
        _loading = false;
    }

    private async void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading ||
            ThemeBox.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string theme)
        {
            return;
        }

        await ViewModel.SetThemeAsync(theme);
    }
}
