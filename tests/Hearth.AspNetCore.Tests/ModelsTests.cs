namespace Hearth.AspNetCore.Tests;

public sealed class ModelsTests : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    public ModelsTests(WebAppFixture fixture) => _client = fixture.Client;

    [Fact]
    public async Task GetModels_Returns200()
    {
        var response = await _client.GetAsync("/v1/models");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetModels_ResponseHasCorrectShape()
    {
        var response = await _client.GetAsync("/v1/models");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("list", body.GetProperty("object").GetString());
        Assert.True(body.GetProperty("data").GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetModels_ModelEntryHasRequiredFields()
    {
        var response = await _client.GetAsync("/v1/models");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var first = body.GetProperty("data")[0];

        Assert.Equal("model", first.GetProperty("object").GetString());
        Assert.Equal("hearth", first.GetProperty("owned_by").GetString());
        Assert.False(string.IsNullOrEmpty(first.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task GetModels_IdMatchesMockModelId()
    {
        var response = await _client.GetAsync("/v1/models");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("data")[0].GetProperty("id").GetString();

        Assert.Equal("mock-model", id);
    }
}
