namespace NexaDesk.Models;

public sealed class WorkflowDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<WorkflowStep> Steps { get; init; } = Array.Empty<WorkflowStep>();
    public int StepCount => Steps.Count;
    public string StepCountText => $"共 {Steps.Count} 个步骤";
}

public sealed class WorkflowStep
{
    public required string Id { get; init; }
    public required string WorkflowId { get; init; }
    public required string ActionId { get; init; }
    public required string ActionName { get; init; }
    public int Position { get; init; }
    public int DelayAfterMilliseconds { get; init; }
}
