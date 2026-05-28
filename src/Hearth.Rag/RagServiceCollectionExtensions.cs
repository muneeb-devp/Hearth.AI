using Hearth.Rag.Chunking;
using Hearth.Rag.Documents;
using Hearth.Rag.Pipeline;
using Hearth.Rag.VectorStore;
using Hearth.Rag.VectorStore.InMemory;
using Hearth.Rag.VectorStore.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hearth.Rag;

public static class RagServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Hearth RAG pipeline to the service collection.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddHearth(o => { o.Model = "Qwen/Qwen2.5-7B-Instruct-GGUF"; })
    ///         .AddRag(o => { o.ChunkSize = 512; });
    /// </code>
    /// </example>
    public static IHearthBuilder AddRag(
        this IHearthBuilder builder,
        Action<RagOptions>? configure = null)
    {
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }
        else
        {
            builder.Services.AddOptions<RagOptions>();
        }

        builder.Services.AddSingleton<IDocumentChunker>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RagOptions>>().Value;
            return opts.Chunker switch
            {
                ChunkerType.Markdown => new MarkdownChunker(),
                _ => new RecursiveChunker(),
            };
        });

        builder.Services.AddSingleton<IVectorStore>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RagOptions>>().Value;
            return opts.VectorStore switch
            {
                VectorStoreType.Sqlite => new SqliteVectorStore(opts.SqlitePath ?? "hearth-rag.db"),
                _ => new InMemoryVectorStore(),
            };
        });

        builder.Services.AddSingleton<IRagPipeline, RagPipeline>();

        builder.Services.AddSingleton<IDocumentLoader, PlainTextLoader>();
        builder.Services.AddSingleton<IDocumentLoader, MarkdownLoader>();
        builder.Services.AddSingleton<IDocumentLoader, HtmlLoader>();
        builder.Services.AddSingleton<DocumentLoaderRegistry>();

        return builder;
    }
}
