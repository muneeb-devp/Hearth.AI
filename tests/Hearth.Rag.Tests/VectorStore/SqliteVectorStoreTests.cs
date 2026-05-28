namespace Hearth.Rag.Tests.VectorStore;

public sealed class SqliteVectorStoreTests : IDisposable
{
    private readonly SqliteVectorStore _store = new(":memory:");
    private static readonly float[] VecA = [1f, 0f, 0f, 0f];
    private static readonly float[] VecB = [0f, 1f, 0f, 0f];
    private static readonly float[] VecC = [0.9f, 0.1f, 0f, 0f];

    [Fact]
    public async Task UpsertThenCount_ReturnsCorrectCount()
    {
        await _store.UpsertAsync("a", VecA, "text a", null);
        await _store.UpsertAsync("b", VecB, "text b", null);

        Assert.Equal(2L, await _store.CountAsync());
    }

    [Fact]
    public async Task Search_ReturnsMostSimilarFirst()
    {
        await _store.UpsertAsync("a", VecA, "text a", null);
        await _store.UpsertAsync("b", VecB, "text b", null);
        await _store.UpsertAsync("c", VecC, "text c", null);

        var results = await _store.SearchAsync(VecA, topK: 2, minScore: 0f);

        Assert.Equal(2, results.Count);
        Assert.Equal("a", results[0].Id);
    }

    [Fact]
    public async Task Search_TopKLimitsResults()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.UpsertAsync($"id{i}", VecA, $"text {i}", null);
        }

        var results = await _store.SearchAsync(VecA, topK: 2, minScore: 0f);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Delete_RemovesEntry()
    {
        await _store.UpsertAsync("x", VecA, "to delete", null);
        await _store.DeleteAsync("x");

        Assert.Equal(0L, await _store.CountAsync());
    }

    [Fact]
    public async Task Upsert_OverwritesExistingEntry()
    {
        await _store.UpsertAsync("key", VecA, "original", null);
        await _store.UpsertAsync("key", VecB, "updated", null);

        Assert.Equal(1L, await _store.CountAsync());

        var results = await _store.SearchAsync(VecB, topK: 1, minScore: 0f);
        Assert.Equal("updated", results[0].Text);
    }

    [Fact]
    public async Task RoundTrip_PreservesText()
    {
        const string original = "Preserved text content.";
        await _store.UpsertAsync("rt", VecA, original, null);

        var results = await _store.SearchAsync(VecA, topK: 1, minScore: 0f);
        Assert.Equal(original, results[0].Text);
    }

    public void Dispose() => _store.Dispose();
}
