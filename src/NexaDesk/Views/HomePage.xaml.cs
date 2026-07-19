using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NexaDesk.ViewModels;

namespace NexaDesk.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadAsync();
}
