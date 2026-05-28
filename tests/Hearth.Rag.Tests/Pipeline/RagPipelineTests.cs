using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hearth.Rag.Tests.Pipeline;

public sealed class RagPipelineTests
{
    private static readonly float[] FixedEmbedding = [1f, 0f, 0f, 0f];

    private static RagPipeline BuildPipeline(
        IVectorStore? store = null,
        IChatClient? chat = null,
        IDocumentChunker? chunker = null,
        RagOptions? opts = null)
    {
        return new RagPipeline(
            new FakeEmbedder(),
            chat ?? new FakeChatClient("answer"),
            store ?? new InMemoryVectorStore(),
            chunker ?? new RecursiveChunker(),
            Options.Create(opts ?? new RagOptions()),
            NullLogger<RagPipeline>.Instance);
    }

    [Fact]
    public async Task IndexAsync_StoresChunksInVectorStore()
    {
        var store = new InMemoryVectorStore();
        var pipeline = BuildPipeline(store: store, opts: new RagOptions { ChunkSize = 10 });

        var text = string.Join("\n\n", Enumerable.Repeat("word ", 200));
        await pipeline.IndexAsync(text);

        Assert.True(await store.CountAsync() > 0);
    }

    [Fact]
    public async Task AskAsync_ReturnsAnswerFromChatClient()
    {
        var chat = new FakeChatClient("This is the answer.");
        var pipeline = BuildPipeline(chat: chat);

        await pipeline.IndexAsync("The sky is blue.");
        var result = await pipeline.AskAsync("What color is the sky?");

        Assert.Equal("This is the answer.", result.Answer);
    }

    [Fact]
    public async Task AskAsync_PopulatesSources()
    {
        var pipeline = BuildPipeline();

        await pipeline.IndexAsync("Paris is the capital of France.");
        var result = await pipeline.AskAsync("What is the capital of France?");

        Assert.NotEmpty(result.Sources);
        Assert.Contains(result.Sources, s => s.Text.Contains("Paris"));
    }

    [Fact]
    public async Task AskAsync_CustomSystemPrompt_OverridesTemplate()
    {
        string? capturedSystem = null;
        var chat = new FakeChatClient("ok", onCall: msgs =>
        {
            capturedSystem = msgs.FirstOrDefault(m => m.Role == ChatRole.System)?.Text;
        });

        var pipeline = BuildPipeline(chat: chat);
        await pipeline.IndexAsync("Some content.");
        await pipeline.AskAsync("question", new RagQueryOptions { SystemPrompt = "CUSTOM" });

        Assert.Equal("CUSTOM", capturedSystem);
    }

    [Fact]
    public async Task IndexDocumentAsync_UsesDocumentContent()
    {
        var store = new InMemoryVectorStore();
        var pipeline = BuildPipeline(store: store);

        var doc = new PlainDocument { Content = "Document body.", Source = "test.txt" };
        await pipeline.IndexDocumentAsync(doc);

        Assert.Equal(1L, await store.CountAsync());
    }

    // --- Fakes ---

    private sealed class FakeEmbedder : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata => new("fake", null, null, 4);

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var list = values
                .Select(_ => new Embedding<float>(FixedEmbedding.AsMemory()))
                .ToList();
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private sealed class FakeChatClient(
        string response,
        Action<IList<ChatMessage>>? onCall = null) : IChatClient
    {
        public ChatClientMetadata Metadata => new("fake", null, null);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var msgs = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();
            onCall?.Invoke(msgs);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => EmptyStream();

        private static async IAsyncEnumerable<ChatResponseUpdate> EmptyStream()
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
