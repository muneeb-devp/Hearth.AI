namespace Hearth.Rag.Tests.Chunking;

public sealed class RecursiveChunkerTests
{
    private readonly RecursiveChunker _chunker = new();

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var opts = new ChunkerOptions { ChunkSize = 512 };
        var chunks = _chunker.Chunk("Hello world", opts).ToList();

        Assert.Single(chunks);
        Assert.Equal("Hello world", chunks[0].Text);
    }

    [Fact]
    public void Chunk_LongText_AllChunksWithinSize()
    {
        var text = string.Join("\n\n", Enumerable.Range(0, 20)
            .Select(i => $"Paragraph {i}: " + new string('a', 80)));

        var opts = new ChunkerOptions { ChunkSize = 50, Overlap = 5 };
        var chunks = _chunker.Chunk(text, opts).ToList();

        Assert.True(chunks.Count > 1, "Expected multiple chunks for long text");
        foreach (var chunk in chunks)
        {
            Assert.True(chunk.Text.Length / 4 <= opts.ChunkSize + opts.Overlap,
                $"Chunk too large: {chunk.Text.Length / 4} tokens");
        }
    }

    [Fact]
    public void Chunk_LongText_TotalCoverageIsComplete()
    {
        var paragraphs = Enumerable.Range(0, 10)
            .Select(i => $"Sentence {i} about topic {i}.");
        var text = string.Join("\n\n", paragraphs);

        var opts = new ChunkerOptions { ChunkSize = 10, Overlap = 0 };
        var chunks = _chunker.Chunk(text, opts).ToList();

        for (int i = 0; i < 10; i++)
        {
            Assert.True(chunks.Any(c => c.Text.Contains($"Sentence {i}")),
                $"Sentence {i} missing from all chunks");
        }
    }

    [Fact]
    public void Chunk_EmptyText_ReturnsNoChunks()
    {
        var chunks = _chunker.Chunk(string.Empty, new ChunkerOptions()).ToList();
        Assert.Empty(chunks);
    }

    [Fact]
    public void Chunk_StartIndex_IsNonNegative()
    {
        var text = string.Join("\n\n", Enumerable.Repeat("word ", 300));
        var opts = new ChunkerOptions { ChunkSize = 20, Overlap = 0 };
        var chunks = _chunker.Chunk(text, opts).ToList();

        Assert.All(chunks, c => Assert.True(c.StartIndex >= 0));
        Assert.All(chunks, c => Assert.True(c.EndIndex > c.StartIndex));
    }
}
