using Hearth.Rag.Chunking;
using Hearth.Rag.Documents;
using Hearth.Rag.VectorStore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hearth.Rag.Pipeline;

internal sealed class RagPipeline(
    IEmbeddingGenerator<string, Embedding<float>> embedder,
    IChatClient chatClient,
    IVectorStore vectorStore,
    IDocumentChunker chunker,
    IOptions<RagOptions> options,
    ILogger<RagPipeline> logger) : IRagPipeline
{
    private readonly RagOptions _opts = options.Value;

    public async Task IndexAsync(string text, object? metadata = null, CancellationToken ct = default)
    {
        var chunkOpts = new ChunkerOptions { ChunkSize = _opts.ChunkSize, Overlap = _opts.ChunkOverlap };
        var chunks = chunker.Chunk(text, chunkOpts).ToList();
        logger.LogInformation("Indexing {ChunkCount} chunks", chunks.Count);

        foreach (var chunk in chunks)
        {
            var embeddings = await embedder.GenerateAsync([chunk.Text], cancellationToken: ct);
            var id = $"{Guid.NewGuid():N}";
            await vectorStore.UpsertAsync(id, embeddings[0].Vector.ToArray(), chunk.Text, metadata, ct);
        }
    }

    public Task IndexDocumentAsync(IDocument document, CancellationToken ct = default) =>
        IndexAsync(document.Content,
            new Dictionary<string, object?> { ["source"] = document.Source }.Concat(
                document.Metadata).ToDictionary(kv => kv.Key, kv => kv.Value),
            ct);

    public async Task<RagResult> AskAsync(string question, RagQueryOptions? opts = null, CancellationToken ct = default)
    {
        opts ??= new RagQueryOptions();

        var queryEmbeddings = await embedder.GenerateAsync([question], cancellationToken: ct);
        var sources = await vectorStore.SearchAsync(queryEmbeddings[0].Vector.ToArray(), opts.TopK, opts.MinScore, ct);

        var context = string.Join("\n\n---\n\n", sources.Select(s => s.Text));
        var systemPrompt = opts.SystemPrompt ?? string.Format(_opts.ContextPromptTemplate, context);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, question),
        };

        var response = await chatClient.GetResponseAsync(messages, opts.ChatOptions, ct);
        return new RagResult(response.Text, sources, response);
    }
}
