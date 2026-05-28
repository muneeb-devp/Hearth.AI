namespace Hearth.Rag.Chunking;

public readonly record struct TextChunk(string Text, int StartIndex, int EndIndex);
