using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaDesk.Models;

namespace NexaDesk.ViewModels;

public partial class ActionsViewModel : ObservableObject
{
    public ObservableCollection<ActionDefinition> Actions { get; } = [];

    [ObservableProperty]
    private string query = string.Empty;

    [ObservableProperty]
    private string statusMessage = "输入名称、分类或描述进行搜索。";

    public async Task SearchAsync()
    {
        IReadOnlyList<ActionDefinition> results =
            await App.Services.Database.SearchActionsAsync(Query);

        Actions.Clear();
        foreach (ActionDefinition action in results)
        {
            Actions.Add(action);
        }

        StatusMessage = $"共 {Actions.Count} 个结果";
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
        await SearchAsync();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ActionDefinition? action)
    {
        if (action is null)
        {
            return;
        }

        action.IsFavorite = !action.IsFavorite;
        await App.Services.Database.SetFavoriteAsync(action.Id, action.IsFavorite);
        StatusMessage = action.IsFavorite ? "已添加到收藏。" : "已取消收藏。";
        await SearchAsync();
    }
}
