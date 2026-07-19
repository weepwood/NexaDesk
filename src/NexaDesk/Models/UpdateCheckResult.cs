namespace NexaDesk.Models;

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    bool IsRequired,
    string Message,
    string? Version = null,
    Uri? ReleaseUri = null);
