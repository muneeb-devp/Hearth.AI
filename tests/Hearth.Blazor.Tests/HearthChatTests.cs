using System.Runtime.CompilerServices;

namespace Hearth.Blazor.Tests;

public sealed class HearthChatTests : TestContext
{
    public HearthChatTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_EmptyState_When_No_Messages()
    {
        Services.AddSingleton<IChatClient>(new NoOpChatClient());
        var cut = RenderComponent<HearthChat>();

        Assert.Contains("hearth-empty-state", cut.Markup);
    }

    [Fact]
    public void Renders_Custom_EmptyState_Content()
    {
        Services.AddSingleton<IChatClient>(new NoOpChatClient());
        var cut = RenderComponent<HearthChat>(p => p
            .Add(x => x.EmptyStateContent,
                b => b.AddMarkupContent(0, "<p id='custom-empty'>Nothing here</p>")));

        Assert.Contains("custom-empty", cut.Markup);
    }

    [Fact]
    public void SendMessage_AddsUserEntry()
    {
        Services.AddSingleton<IChatClient>(new InstantChatClient("Hello back"));
        var cut = RenderComponent<HearthChat>();

        cut.Find("textarea").Change("Hello!");
        cut.Find("button.hearth-send-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Hello!", cut.Markup));
    }

    [Fact]
    public void SendMessage_ShowsAssistantResponse()
    {
        Services.AddSingleton<IChatClient>(new InstantChatClient("I am Hearth."));
        var cut = RenderComponent<HearthChat>();

        cut.Find("textarea").Change("Who are you?");
        cut.Find("button.hearth-send-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("I am Hearth.", cut.Markup));
    }

    [Fact]
    public void SendMessage_AccumulatesMultipleTokens()
    {
        Services.AddSingleton<IChatClient>(new InstantChatClient("Hello", " ", "world", "!"));
        var cut = RenderComponent<HearthChat>();

        cut.Find("textarea").Change("Hi");
        cut.Find("button.hearth-send-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Hello world!", cut.Markup));
    }

    [Fact]
    public async Task ClearHistory_RemovesAllEntries()
    {
        Services.AddSingleton<IChatClient>(new InstantChatClient("ok"));
        var cut = RenderComponent<HearthChat>();

        cut.Find("textarea").Change("Test");
        cut.Find("button.hearth-send-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("hearth-empty-state", cut.Markup));

        // ClearHistory calls StateHasChanged which must run on the Blazor dispatcher
        await cut.InvokeAsync(() => cut.Instance.ClearHistory());

        cut.WaitForAssertion(() =>
            Assert.Contains("hearth-empty-state", cut.Markup));
    }

    [Fact]
    public void Cancel_StopsStreaming()
    {
        var barrier = new TaskCompletionSource();
        Services.AddSingleton<IChatClient>(new BlockingChatClient(barrier));
        var cut = RenderComponent<HearthChat>();

        cut.Find("textarea").Change("question");
        cut.Find("button.hearth-send-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("hearth-spinner", cut.Markup));

        cut.Instance.Cancel();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("hearth-spinner", cut.Markup));
    }

    // ─── Fakes ───────────────────────────────────────────────────────────────

    private sealed class NoOpChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("fake", null, null);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)));

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private sealed class InstantChatClient(params string[] tokens) : IChatClient
    {
        public ChatClientMetadata Metadata => new("fake", null, null);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent(token)],
                };
            }
            await Task.CompletedTask;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, string.Concat(tokens))));

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private sealed class BlockingChatClient(TaskCompletionSource barrier) : IChatClient
    {
        public ChatClientMetadata Metadata => new("fake", null, null);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent("...thinking...")],
            };
            await barrier.Task.WaitAsync(cancellationToken);
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "response")));

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
