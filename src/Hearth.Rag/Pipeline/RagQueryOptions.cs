using Microsoft.Extensions.AI;

namespace Hearth.Rag.Pipeline;

public sealed class RagQueryOptions
{
    public int TopK { get; set; } = 5;
    public float MinScore { get; set; } = 0f;
    public string? SystemPrompt { get; set; }
    public ChatOptions? ChatOptions { get; set; }
}
