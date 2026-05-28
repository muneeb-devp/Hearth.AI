namespace Hearth.Rag.Chunking;

public sealed class ChunkerOptions
{
    public int ChunkSize { get; set; } = 512;
    public int Overlap { get; set; } = 50;
}
