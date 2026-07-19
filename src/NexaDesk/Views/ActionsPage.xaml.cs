using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NexaDesk.ViewModels;

namespace NexaDesk.Views;

public sealed partial class ActionsPage : Page
{
    private CancellationTokenSource? _searchDelay;
    public ActionsViewModel ViewModel { get; } = new();

    public ActionsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.SearchAsync();

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDelay?.Cancel();
        _searchDelay?.Dispose();
        _searchDelay = new CancellationTokenSource();

        try
        {
            await Task.Delay(180, _searchDelay.Token);
            ViewModel.Query = SearchBox.Text;
            await ViewModel.SearchAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _searchDelay?.Cancel();
        _searchDelay?.Dispose();
    }
}
