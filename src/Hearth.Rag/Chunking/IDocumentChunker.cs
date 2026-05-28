namespace Hearth.Rag.Chunking;

public interface IDocumentChunker
{
    IEnumerable<TextChunk> Chunk(string text, ChunkerOptions options);
}
