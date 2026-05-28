namespace Hearth.Rag.VectorStore;

public interface IVectorStore
{
    Task UpsertAsync(string id, float[] embedding, string text, object? metadata, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] query, int topK = 5, float minScore = 0f, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<long> CountAsync(CancellationToken ct = default);
}
