using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NexaDesk.Models;

namespace NexaDesk.ViewModels;

public partial class CommandPaletteViewModel : ObservableObject
{
    public ObservableCollection<ActionDefinition> Results { get; } = [];

    [ObservableProperty]
    private string statusMessage = "输入动作或应用名称";

    public async Task SearchAsync(string query)
    {
        IReadOnlyList<ActionDefinition> actions =
            await App.Services.Database.SearchActionsAsync(query, 20);

        Results.Clear();
        foreach (ActionDefinition action in actions)
        {
            Results.Add(action);
        }

        StatusMessage = Results.Count == 0
            ? "没有找到匹配项"
            : $"{Results.Count} 个结果";
    }

    public async Task<ExecutionResult> ExecuteAsync(ActionDefinition action)
    {
        ExecutionResult result = await App.Services.Actions.ExecuteAsync(action);
        StatusMessage = result.Message;
        return result;
    }
}
