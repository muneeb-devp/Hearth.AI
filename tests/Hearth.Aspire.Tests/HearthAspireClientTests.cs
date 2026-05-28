using Hearth.Aspire;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hearth.Aspire.Tests;

public sealed class HearthAspireClientTests
{
    [Fact]
    public void AddHearth_RegistersIChatClient()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["ConnectionStrings:ai"] = "http://localhost:5000/v1";
        builder.AddHearth("ai");

        using var host = builder.Build();
        var chatClient = host.Services.GetService<IChatClient>();
        Assert.NotNull(chatClient);
    }

    [Fact]
    public void AddHearth_ThrowsWhenConnectionStringMissing()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.AddHearth("ai");

        using var host = builder.Build();
        var ex = Assert.Throws<InvalidOperationException>(
            () => host.Services.GetRequiredService<IChatClient>());

        Assert.Contains("ai", ex.Message);
    }

    [Fact]
    public void AddHearth_ThrowsOnNullBuilder()
    {
        IHostApplicationBuilder builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.AddHearth("ai"));
    }

    [Fact]
    public void AddHearth_ThrowsOnEmptyConnectionName()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        Assert.Throws<ArgumentException>(() => builder.AddHearth(""));
    }

    [Fact]
    public void AddHearth_ReturnsSameBuilder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration["ConnectionStrings:ai"] = "http://localhost:5000/v1";

        var result = builder.AddHearth("ai");
        Assert.Same(builder, result);
    }
}
