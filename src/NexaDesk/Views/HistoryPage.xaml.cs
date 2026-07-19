using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NexaDesk.ViewModels;

namespace NexaDesk.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; } = new();

    public HistoryPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadAsync();
}
