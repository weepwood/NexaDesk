namespace NexaDesk.Models;

public sealed class ExecutionHistoryItem
{
    public long Id { get; init; }
    public required string ActionId { get; init; }
    public required string ActionName { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset ExecutedAt { get; init; }
    public string StatusText => Success ? "成功" : "失败";
}
