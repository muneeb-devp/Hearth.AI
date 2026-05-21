namespace Hearth.AspNetCore.Tests;

public sealed class EmbeddingsTests : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    public EmbeddingsTests(WebAppFixture fixture) => _client = fixture.Client;

    // ── integration ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostEmbeddings_WithStringInput_Returns200()
    {
        var response = await _client.PostAsync("/v1/embeddings", JsonBody(new
        {
            model = "mock-model",
            input = "hello world",
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostEmbeddings_WithArrayInput_Returns200()
    {
        var response = await _client.PostAsync("/v1/embeddings", JsonBody(new
        {
            model = "mock-model",
            input = new[] { "hello", "world" },
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostEmbeddings_ResponseHasCorrectShape()
    {
        var response = await _client.PostAsync("/v1/embeddings", JsonBody(new
        {
            model = "mock-model",
            input = "hello",
        }));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("list", body.GetProperty("object").GetString());
        Assert.Equal("embedding", body.GetProperty("data")[0].GetProperty("object").GetString());
        Assert.Equal(0, body.GetProperty("data")[0].GetProperty("index").GetInt32());
        Assert.True(body.GetProperty("data")[0].GetProperty("embedding").GetArrayLength() > 0);
    }

    [Fact]
    public async Task PostEmbeddings_ArrayInput_ReturnsSameCountAsInput()
    {
        var response = await _client.PostAsync("/v1/embeddings", JsonBody(new
        {
            model = "mock-model",
            input = new[] { "a", "b", "c" },
        }));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task PostEmbeddings_InvalidInputType_Returns400()
    {
        var response = await _client.PostAsync("/v1/embeddings", JsonBody(new
        {
            model = "mock-model",
            input = 42,
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── unit: ParseInputs ────────────────────────────────────────────────────

    [Fact]
    public void ParseInputs_StringInput_ReturnsSingleElement()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("\"hello world\"");
        var result = EmbeddingsEndpoint.ParseInputs(json);
        Assert.Single(result);
        Assert.Equal("hello world", result[0]);
    }

    [Fact]
    public void ParseInputs_ArrayInput_ReturnsAllElements()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("[\"a\",\"b\",\"c\"]");
        var result = EmbeddingsEndpoint.ParseInputs(json);
        Assert.Equal(3, result.Count);
        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void ParseInputs_InvalidKind_ThrowsArgumentException()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("42");
        Assert.Throws<ArgumentException>(() => EmbeddingsEndpoint.ParseInputs(json));
    }

    private static StringContent JsonBody(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}
