using Microsoft.Extensions.AI;

namespace Hearth.AspNetCore.Tests;

public sealed class ChatCompletionsTests : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    public ChatCompletionsTests(WebAppFixture fixture) => _client = fixture.Client;

    // ── integration ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostChatCompletions_ValidRequest_Returns200()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = new[] { new { role = "user", content = "hello" } },
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostChatCompletions_ResponseHasCorrectShape()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = new[] { new { role = "user", content = "hello" } },
        }));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("chat.completion", body.GetProperty("object").GetString());
        Assert.Equal("assistant", body.GetProperty("choices")[0].GetProperty("message").GetProperty("role").GetString());
        Assert.Equal("stop", body.GetProperty("choices")[0].GetProperty("finish_reason").GetString());
    }

    [Fact]
    public async Task PostChatCompletions_EchoesUserMessage()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = new[] { new { role = "user", content = "ping" } },
        }));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var content = body.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        Assert.Contains("ping", content);
    }

    [Fact]
    public async Task PostChatCompletions_EmptyMessages_Returns400()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = Array.Empty<object>(),
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostChatCompletions_Streaming_ReturnsTextEventStreamContentType()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = new[] { new { role = "user", content = "hi" } },
            stream = true,
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostChatCompletions_Streaming_ContainsDoneEvent()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = new[] { new { role = "user", content = "hi" } },
            stream = true,
        }));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("data: [DONE]", body);
    }

    [Fact]
    public async Task PostChatCompletions_Streaming_ChunksHaveCorrectObject()
    {
        var response = await _client.PostAsync("/v1/chat/completions", JsonBody(new
        {
            model = "mock-model",
            messages = new[] { new { role = "user", content = "hi" } },
            stream = true,
        }));

        var body = await response.Content.ReadAsStringAsync();
        var dataLines = body.Split('\n')
            .Where(l => l.StartsWith("data: {", StringComparison.Ordinal))
            .Select(l => JsonSerializer.Deserialize<JsonElement>(l[6..]))
            .ToList();

        Assert.NotEmpty(dataLines);
        Assert.All(dataLines, chunk =>
            Assert.Equal("chat.completion.chunk", chunk.GetProperty("object").GetString()));
    }

    // ── unit: MapMessages ────────────────────────────────────────────────────

    [Fact]
    public void MapMessages_SystemRole_MapsToSystemChatRole()
    {
        var result = ChatCompletionsEndpoint.MapMessages([new OpenAiMessage { Role = "system", Content = "sys" }]);
        Assert.Equal(ChatRole.System, result[0].Role);
    }

    [Fact]
    public void MapMessages_AssistantRole_MapsToAssistantChatRole()
    {
        var result = ChatCompletionsEndpoint.MapMessages([new OpenAiMessage { Role = "assistant", Content = "a" }]);
        Assert.Equal(ChatRole.Assistant, result[0].Role);
    }

    [Fact]
    public void MapMessages_UnknownRole_MapsToUserChatRole()
    {
        var result = ChatCompletionsEndpoint.MapMessages([new OpenAiMessage { Role = "unknown", Content = "u" }]);
        Assert.Equal(ChatRole.User, result[0].Role);
    }

    private static StringContent JsonBody(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}
