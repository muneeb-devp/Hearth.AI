using Microsoft.Extensions.AI;

namespace Hearth.Blazor.Models;

public sealed class ChatEntry
{
    public required string Id { get; init; }
    public required ChatRole Role { get; init; }
    public string Content { get; set; } = string.Empty;
    public bool IsStreaming { get; set; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
