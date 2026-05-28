namespace Hearth.Rag.Documents;

public interface IDocument
{
    string Content { get; }
    string? Source { get; }
    IReadOnlyDictionary<string, object?> Metadata { get; }
}
