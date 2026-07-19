using NexaDesk.Models;

namespace NexaDesk;

public sealed class WorkflowService(
    DatabaseService database,
    ActionExecutionService actions)
{
    public Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync() =>
        database.GetWorkflowsAsync();

    public Task CreateAsync(
        string name,
        IReadOnlyList<ActionDefinition> selectedActions) =>
        database.CreateWorkflowAsync(name, selectedActions);

    public Task DeleteAsync(string id) => database.DeleteWorkflowAsync(id);

    public async Task<ExecutionResult> ExecuteAsync(
        WorkflowDefinition workflow,
        CancellationToken cancellationToken = default)
    {
        foreach (WorkflowStep step in workflow.Steps.OrderBy(static item => item.Position))
        {
            cancellationToken.ThrowIfCancellationRequested();

            ActionDefinition? action = await database.GetActionAsync(step.ActionId);
            if (action is null)
            {
                return ExecutionResult.Fail($"找不到动作：{step.ActionName}");
            }

            ExecutionResult result = await actions.ExecuteAsync(action, cancellationToken);
            if (!result.Success)
            {
                return ExecutionResult.Fail(
                    $"工作流在“{step.ActionName}”处停止：{result.Message}");
            }

            if (step.DelayAfterMilliseconds > 0)
            {
                await Task.Delay(step.DelayAfterMilliseconds, cancellationToken);
            }
        }

        return ExecutionResult.Ok($"工作流“{workflow.Name}”执行完成。");
    }
}
