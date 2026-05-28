using System.Text.Json;
using Hearth.Rag.VectorStore.InMemory;
using Microsoft.Data.Sqlite;

namespace Hearth.Rag.VectorStore.Sqlite;

internal sealed class SqliteVectorStore : IVectorStore, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _initialized;

    public SqliteVectorStore(string dbPath)
    {
        var cs = dbPath == ":memory:"
            ? "Data Source=:memory:"
            : $"Data Source={dbPath}";
        _connection = new SqliteConnection(cs);
        _connection.Open();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS hearth_vectors (
                id       TEXT PRIMARY KEY,
                text     TEXT NOT NULL,
                embedding BLOB NOT NULL,
                metadata TEXT
            );
            """;
        cmd.ExecuteNonQuery();
        _initialized = true;
    }

    public Task UpsertAsync(string id, float[] embedding, string text, object? metadata, CancellationToken ct = default)
    {
        EnsureInitialized();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO hearth_vectors (id, text, embedding, metadata)
            VALUES ($id, $text, $embedding, $metadata)
            ON CONFLICT(id) DO UPDATE SET text=excluded.text, embedding=excluded.embedding, metadata=excluded.metadata;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$text", text);
        cmd.Parameters.AddWithValue("$embedding", FloatsToBytes(embedding));
        cmd.Parameters.AddWithValue("$metadata",
            metadata is null ? DBNull.Value : JsonSerializer.Serialize(metadata));
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] query, int topK = 5, float minScore = 0f, CancellationToken ct = default)
    {
        EnsureInitialized();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, text, embedding, metadata FROM hearth_vectors";

        var results = new List<VectorSearchResult>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetString(0);
            var text = reader.GetString(1);
            var embeddingBytes = (byte[])reader.GetValue(2);
            var metaJson = reader.IsDBNull(3) ? null : reader.GetString(3);

            var storedEmbedding = BytesToFloats(embeddingBytes);
            var score = InMemoryVectorStore.CosineSimilarity(query, storedEmbedding);

            if (score >= minScore)
            {
                object? meta = metaJson is null ? null : JsonSerializer.Deserialize<object>(metaJson);
                results.Add(new VectorSearchResult(id, text, score, meta));
            }
        }

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(
            results.OrderByDescending(r => r.Score).Take(topK).ToList());
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        EnsureInitialized();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM hearth_vectors WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<long> CountAsync(CancellationToken ct = default)
    {
        EnsureInitialized();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM hearth_vectors";
        return Task.FromResult((long)cmd.ExecuteScalar()!);
    }

    public void Dispose() => _connection.Dispose();

    private static byte[] FloatsToBytes(float[] floats)
    {
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloats(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
