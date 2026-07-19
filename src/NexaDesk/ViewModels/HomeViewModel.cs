using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaDesk.Models;

namespace NexaDesk.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    public ObservableCollection<ActionDefinition> Favorites { get; } = [];
    public ObservableCollection<ExecutionHistoryItem> RecentHistory { get; } = [];

    [ObservableProperty]
    private string statusMessage = "就绪";

    public async Task LoadAsync()
    {
        Favorites.Clear();
        IReadOnlyList<ActionDefinition> actions =
            await App.Services.Database.GetFavoriteActionsAsync();

        if (actions.Count == 0)
        {
            actions = await App.Services.Database.SearchActionsAsync(string.Empty, 6);
        }

        foreach (ActionDefinition action in actions)
        {
            Favorites.Add(action);
        }

        RecentHistory.Clear();
        foreach (ExecutionHistoryItem item in await App.Services.Database.GetHistoryAsync(8))
        {
            RecentHistory.Add(item);
        }
    }

    [RelayCommand]
    private async Task RunActionAsync(ActionDefinition? action)
    {
        if (action is null)
        {
            return;
        }

        ExecutionResult result = await App.Services.Actions.ExecuteAsync(action);
        StatusMessage = result.Message;
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenPalette() => App.MainWindow?.ShowCommandPalette();
}
