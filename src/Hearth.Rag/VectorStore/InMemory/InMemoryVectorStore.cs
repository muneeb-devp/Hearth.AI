using System.Collections.Concurrent;

namespace Hearth.Rag.VectorStore.InMemory;

internal sealed class InMemoryVectorStore : IVectorStore
{
    private readonly record struct Entry(float[] Embedding, string Text, object? Metadata);
    private readonly ConcurrentDictionary<string, Entry> _store = new();

    public Task UpsertAsync(string id, float[] embedding, string text, object? metadata, CancellationToken ct = default)
    {
        _store[id] = new Entry(embedding, text, metadata);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] query, int topK = 5, float minScore = 0f, CancellationToken ct = default)
    {
        var results = _store
            .Select(kvp => new VectorSearchResult(
                kvp.Key,
                kvp.Value.Text,
                CosineSimilarity(query, kvp.Value.Embedding),
                kvp.Value.Metadata))
            .Where(r => r.Score >= minScore)
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<long> CountAsync(CancellationToken ct = default) =>
        Task.FromResult((long)_store.Count);

    internal static float CosineSimilarity(float[] a, float[] b)
    {
        int len = Math.Min(a.Length, b.Length);
        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < len; i++)
        {
            dot   += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB) + 1e-8f);
    }
}
