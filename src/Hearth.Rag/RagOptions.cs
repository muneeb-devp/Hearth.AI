namespace Hearth.Rag;

public sealed class RagOptions
{
    public VectorStoreType VectorStore { get; set; } = VectorStoreType.InMemory;
    public string? SqlitePath { get; set; }
    public int ChunkSize { get; set; } = 512;
    public int ChunkOverlap { get; set; } = 50;
    public ChunkerType Chunker { get; set; } = ChunkerType.Recursive;
    public string ContextPromptTemplate { get; set; } = Pipeline.RagDefaults.ContextTemplate;
}
