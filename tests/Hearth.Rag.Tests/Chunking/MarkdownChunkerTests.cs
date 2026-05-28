namespace Hearth.Rag.Tests.Chunking;

public sealed class MarkdownChunkerTests
{
    private readonly MarkdownChunker _chunker = new();
    private readonly ChunkerOptions _opts = new() { ChunkSize = 512 };

    private const string SampleMarkdown = """
        # Introduction
        This is the intro section with some text.

        ## Getting Started
        Here is how you get started with this library.

        ## Advanced Usage
        Advanced topics go here. This section is longer and contains more details.
        """;

    [Fact]
    public void Chunk_MarkdownWithHeadings_EachChunkContainsHeading()
    {
        var chunks = _chunker.Chunk(SampleMarkdown, _opts).ToList();

        Assert.True(chunks.Count >= 2);
        var advancedChunk = chunks.FirstOrDefault(c => c.Text.Contains("Advanced Usage"));
        Assert.False(advancedChunk == default, "Expected a chunk containing 'Advanced Usage'");
        Assert.StartsWith("## Advanced Usage", advancedChunk.Text);
    }

    [Fact]
    public void Chunk_MarkdownWithHeadings_HeadingAppearsInChunk()
    {
        var chunks = _chunker.Chunk(SampleMarkdown, _opts).ToList();

        var gettingStarted = chunks.FirstOrDefault(c => c.Text.Contains("Getting Started"));
        Assert.False(gettingStarted == default, "Expected a chunk containing 'Getting Started'");
        Assert.Contains("## Getting Started", gettingStarted.Text);
    }

    [Fact]
    public void Chunk_PlainText_ChunksAreProduced()
    {
        var text = "No headings here. Just some plain text content.";
        var chunks = _chunker.Chunk(text, _opts).ToList();

        Assert.NotEmpty(chunks);
        Assert.Equal(text, chunks[0].Text);
    }

    [Fact]
    public void Chunk_LargeSection_FallsBackToRecursive()
    {
        var bigSection = "## Big Section\n" + string.Join("\n", Enumerable.Repeat("word ", 600));
        var opts = new ChunkerOptions { ChunkSize = 10, Overlap = 0 };

        var chunks = _chunker.Chunk(bigSection, opts).ToList();

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.Contains("## Big Section", c.Text));
    }
}
