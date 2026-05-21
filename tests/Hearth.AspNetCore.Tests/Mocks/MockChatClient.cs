using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Hearth.AspNetCore.Tests.Mocks;

public sealed class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("hearth-test", null, "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var lastUser = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Echo: {lastUser}")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent("Hello")] };
        await Task.Yield();
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent(" world")] };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null && serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        return null;
    }

    public void Dispose() { }
}
