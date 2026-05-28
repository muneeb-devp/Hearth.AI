namespace Hearth.Rag.Documents;

public sealed class PlainDocument : IDocument
{
    public required string Content { get; init; }
    public string? Source { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
