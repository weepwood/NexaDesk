namespace NexaDesk.Models;

public enum ActionKind
{
    LaunchFile = 0,
    LaunchUri = 1,
    OpenFolder = 2,
    WindowTopMost = 3,
    WindowCenter = 4,
    LockWorkstation = 5,
    RunCommand = 6
}

public sealed class ActionDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public ActionKind Kind { get; init; }
    public string Target { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public string Category { get; init; } = "其他";
    public string IconGlyph { get; init; } = "\uE945";
    public bool IsFavorite { get; set; }
    public long UsageCount { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
}

public sealed record ExecutionResult(bool Success, string Message)
{
    public static ExecutionResult Ok(string message = "操作已执行。") => new(true, message);
    public static ExecutionResult Fail(string message) => new(false, message);
}
