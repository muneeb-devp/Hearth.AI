namespace Hearth.Rag.VectorStore;

public sealed record VectorSearchResult(string Id, string Text, float Score, object? Metadata);
