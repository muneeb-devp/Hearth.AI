using Hearth.AspNetCore.Tests.Mocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Hearth.AspNetCore.Tests;

/// <summary>
/// Shared test fixture that hosts an in-memory ASP.NET Core app with Hearth endpoints.
/// Uses <see cref="MockChatClient"/> and <see cref="MockEmbeddingGenerator"/> — no real model needed.
/// </summary>
public sealed class WebAppFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public HttpClient Client { get; private set; } = null!;
    public MockChatClient ChatClient { get; } = new();
    public MockEmbeddingGenerator EmbeddingGenerator { get; } = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<IChatClient>(ChatClient);
        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(EmbeddingGenerator);

        _app = builder.Build();
        _app.MapHearth();

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
