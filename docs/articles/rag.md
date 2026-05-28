# Retrieval-augmented generation (RAG)

`Hearth.AI.Rag` adds a local RAG pipeline on top of the base `Hearth.AI` package. It chunks and embeds documents using the same model runtime that handles chat, stores vectors in an in-memory or SQLite store, and answers questions by retrieving relevant chunks before calling the model.

Everything runs in-process. There are no external services, no API keys, and no round-trips over the network.

## Install

```bash
dotnet add package Hearth.AI.Rag
```

## Register the pipeline

Chain `.AddRag()` from the `IHearthBuilder` returned by `AddHearth()`:

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
})
.AddRag(options =>
{
    options.VectorStore = VectorStoreType.InMemory;
    options.ChunkSize = 512;
    options.ChunkOverlap = 50;
    options.Chunker = ChunkerType.Recursive;
});
```

`.AddRag()` registers:

- `IRagPipeline` — the main entry point for indexing and querying
- `IVectorStore` — either in-memory or SQLite, based on your options
- `IDocumentChunker` — splits text into overlapping chunks
- `DocumentLoaderRegistry` and the built-in loaders (`PlainTextLoader`, `MarkdownLoader`, `HtmlLoader`)

## RagOptions reference

| Option | Default | Description |
| --- | --- | --- |
| `VectorStore` | `InMemory` | `InMemory` or `Sqlite` |
| `SqlitePath` | `"hearth-rag.db"` | Database file path, used when `VectorStore` is `Sqlite` |
| `ChunkSize` | `512` | Maximum number of tokens per chunk |
| `ChunkOverlap` | `50` | Overlap between consecutive chunks |
| `Chunker` | `Recursive` | `Recursive` (general text) or `Markdown` (structured docs) |
| `ContextPromptTemplate` | built-in | System prompt template injected with retrieved chunks |

## Basic usage

Inject `IRagPipeline` and index some text, then ask a question:

```csharp
public sealed class DocsService(IRagPipeline rag)
{
    public async Task IndexAsync(CancellationToken cancellationToken = default)
    {
        await rag.IndexAsync(
            "Hearth is a .NET library for local LLM inference. " +
            "It uses LLamaSharp under the hood and exposes Microsoft.Extensions.AI interfaces.",
            metadata: new { Source = "readme" },
            ct: cancellationToken);
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        RagResult result = await rag.AskAsync(question, ct: cancellationToken);
        return result.Answer;
    }
}
```

`IndexAsync` chunks the text, generates embeddings, and writes them to the vector store. `AskAsync` embeds the question, retrieves the top matching chunks, and calls the model with those chunks injected into the system prompt.

## Indexing from files

`DocumentLoaderRegistry` picks the right loader automatically based on file extension:

```csharp
public sealed class IndexingService(IRagPipeline rag, DocumentLoaderRegistry registry)
{
    public async Task IndexDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".txt") || f.EndsWith(".md") || f.EndsWith(".html"));

        foreach (var file in files)
        {
            IDocument document = await registry.LoadAsync(file, cancellationToken);
            await rag.IndexDocumentAsync(document, cancellationToken);
        }
    }
}
```

Supported extensions and their loaders:

| Extension | Loader |
| --- | --- |
| `.txt` | `PlainTextLoader` |
| `.md` | `MarkdownLoader` |
| `.html` | `HtmlLoader` |

You can implement `IDocumentLoader` and register it with the DI container to add support for other formats. `DocumentLoaderRegistry` will pick it up automatically.

## Vector stores

### In-memory

The default. Vectors live in a `List<>` in memory and are gone when the process exits. Use this during development and for short-lived workloads where you re-index on startup.

```csharp
.AddRag(options =>
{
    options.VectorStore = VectorStoreType.InMemory;
});
```

### SQLite

Persists vectors to a SQLite database file. The store is loaded from disk on startup, so indexed documents survive restarts without re-indexing.

```csharp
.AddRag(options =>
{
    options.VectorStore = VectorStoreType.Sqlite;
    options.SqlitePath = "hearth-rag.db";
});
```

Use SQLite when:

- your document set is large or slow to re-index
- the application restarts frequently (e.g. a long-running API)
- you want to pre-index documents in a background job and share the database with the serving process

## Chunking strategies

### Recursive (default)

Splits on paragraph breaks, then sentence breaks, then words — whichever boundary keeps chunks under `ChunkSize` without cutting through sentences. Works well for prose, READMEs, articles, and mixed content.

### Markdown

Splits on Markdown heading boundaries first, so each chunk stays within a logical section. Prefer this when your documents have clear heading structure, such as documentation sites or wikis.

```csharp
.AddRag(options =>
{
    options.Chunker = ChunkerType.Markdown;
    options.ChunkSize = 768;   // larger chunks work well when sections are coherent
    options.ChunkOverlap = 64;
});
```

### Tuning ChunkSize and ChunkOverlap

- Smaller chunks (256–512 tokens) improve retrieval precision but may omit surrounding context that the model needs to form a complete answer.
- Larger chunks (768–1024 tokens) give the model more context per retrieved result but may bring in irrelevant content that dilutes the answer.
- `ChunkOverlap` prevents answers from being split across a chunk boundary. Values between 10% and 15% of `ChunkSize` are a reasonable starting point.

The right values depend on your document structure and the length of the questions you expect. Index the same corpus with a few different settings and compare answer quality before settling on a configuration.

## Querying with RagQueryOptions

```csharp
var result = await rag.AskAsync(
    "What models does Hearth support?",
    new RagQueryOptions
    {
        TopK = 8,         // retrieve more chunks when documents are long
        MinScore = 0.3f,  // discard chunks below this cosine similarity threshold
        SystemPrompt = "You are a technical support assistant for Hearth. Answer concisely.",
    });
```

| Option | Default | Description |
| --- | --- | --- |
| `TopK` | `5` | Maximum number of chunks to retrieve and include in the prompt |
| `MinScore` | `0f` | Minimum cosine similarity — chunks below this threshold are excluded |
| `SystemPrompt` | `null` | Override the system prompt; `null` uses the template from `RagOptions` |
| `ChatOptions` | `null` | Pass through MEA `ChatOptions` (temperature, stop sequences, etc.) |

`MinScore` is useful when the index is large and low-quality matches could mislead the model. A value of `0.3`–`0.5` is a reasonable starting point; tune it based on what retrieval results look like for your documents.

## Inspecting sources

`RagResult.Sources` contains the chunks that were used to construct the answer:

```csharp
RagResult result = await rag.AskAsync("How does Hearth handle GPU offloading?");

Console.WriteLine(result.Answer);
Console.WriteLine();
Console.WriteLine($"Sources ({result.Sources.Count}):");

foreach (VectorSearchResult source in result.Sources)
{
    Console.WriteLine($"  [{source.Score:F3}] {source.Text[..Math.Min(80, source.Text.Length)]}...");
}
```

`VectorSearchResult` exposes:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Unique identifier for the chunk |
| `Text` | `string` | The chunk text that was embedded |
| `Score` | `float` | Cosine similarity to the query |
| `Metadata` | `object?` | Metadata passed to `IndexAsync` or carried from the document |

Use `Metadata` to record the source file path, document title, or any other attribution data you want to surface in your UI.

## End-to-end example: Q&A bot over Markdown docs

This example shows a minimal console app that indexes a folder of Markdown files once and then answers questions interactively.

**Program.cs**

```csharp
using Hearth.Rag;
using Hearth.Rag.Documents;
using Hearth.Rag.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHearth(options =>
        {
            options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
            options.ContextSize = 8192;
        })
        .AddRag(options =>
        {
            options.VectorStore = VectorStoreType.Sqlite;
            options.SqlitePath = "docs-index.db";
            options.Chunker = ChunkerType.Markdown;
            options.ChunkSize = 768;
            options.ChunkOverlap = 64;
        });
    })
    .Build();

var rag = host.Services.GetRequiredService<IRagPipeline>();
var registry = host.Services.GetRequiredService<DocumentLoaderRegistry>();

// Index all Markdown files in ./docs (skip if the database already exists)
if (!File.Exists("docs-index.db"))
{
    Console.WriteLine("Indexing docs...");
    foreach (var file in Directory.GetFiles("./docs", "*.md", SearchOption.AllDirectories))
    {
        var document = await registry.LoadAsync(file);
        await rag.IndexDocumentAsync(document);
    }
    Console.WriteLine("Done.");
}

// Interactive Q&A loop
Console.WriteLine("Ask a question (Ctrl+C to exit):");

while (true)
{
    Console.Write("> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) continue;

    var result = await rag.AskAsync(question, new RagQueryOptions
    {
        TopK = 5,
        MinScore = 0.25f,
    });

    Console.WriteLine();
    Console.WriteLine(result.Answer);
    Console.WriteLine();

    if (result.Sources.Count > 0)
    {
        Console.WriteLine("Sources:");
        foreach (var source in result.Sources)
            Console.WriteLine($"  [{source.Score:F3}] {source.Metadata}");
    }

    Console.WriteLine();
}
```

Pass meaningful metadata when indexing so that sources are easy to interpret:

```csharp
// Instead of IndexDocumentAsync, use IndexAsync directly when you control the metadata
await rag.IndexAsync(
    text: File.ReadAllText(file),
    metadata: new { File = file, Indexed = DateTime.UtcNow });
```

## Performance notes

Embedding and inference both execute on the same model instance. A few things to keep in mind:

- **Indexing is CPU-bound.** Each chunk requires an embedding call. Indexing a large corpus takes time proportional to the number of chunks. Do it once and persist with SQLite rather than re-indexing on every startup.
- **Retrieval is fast.** The in-memory vector store uses a brute-force cosine search, which is plenty fast for corpora up to tens of thousands of chunks. The SQLite store follows the same pattern.
- **Inference follows retrieval.** `AskAsync` runs one embedding call (for the question) and one chat call (with the retrieved chunks injected). Total latency is roughly `embedding_time + inference_time`, same as a normal chat request.
- **Context window budget.** Each retrieved chunk consumes prompt tokens. With `TopK = 5` and `ChunkSize = 512`, you may inject up to ~2,500 tokens of context before your question and system prompt. Make sure `ContextSize` in `HearthOptions` is large enough to accommodate the full prompt.

## See also

- [Getting started](getting-started.md)
- [Configuration](configuration.md)
- [ASP.NET Core integration](aspnetcore.md)
