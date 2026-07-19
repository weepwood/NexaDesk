using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaDesk.Models;

namespace NexaDesk.ViewModels;

public partial class WorkflowsViewModel : ObservableObject
{
    public ObservableCollection<WorkflowDefinition> Workflows { get; } = [];

    [ObservableProperty]
    private string statusMessage = "工作流按顺序执行本地动作。";

    public async Task LoadAsync()
    {
        Workflows.Clear();
        foreach (WorkflowDefinition workflow in await App.Services.Workflows.GetAllAsync())
        {
            Workflows.Add(workflow);
        }
    }

    public async Task CreateAsync(
        string name,
        IReadOnlyList<ActionDefinition> actions)
    {
        await App.Services.Workflows.CreateAsync(name, actions);
        StatusMessage = $"已创建工作流“{name}”。";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RunWorkflowAsync(WorkflowDefinition? workflow)
    {
        if (workflow is null)
        {
            return;
        }

        StatusMessage = $"正在执行“{workflow.Name}”…";
        ExecutionResult result = await App.Services.Workflows.ExecuteAsync(workflow);
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task DeleteWorkflowAsync(WorkflowDefinition? workflow)
    {
        if (workflow is null)
        {
            return;
        }

        await App.Services.Workflows.DeleteAsync(workflow.Id);
        StatusMessage = $"已删除“{workflow.Name}”。";
        await LoadAsync();
    }
}
