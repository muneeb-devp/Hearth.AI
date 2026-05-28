using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hearth.Aspire.Tests;

public sealed class HearthHostingTests
{
    [Fact]
    public void AddHearth_CreatesHearthResource()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var resource = builder.AddHearth("ai");

        Assert.IsType<HearthResource>(resource.Resource);
        Assert.Equal("ai", resource.Resource.Name);
    }

    [Fact]
    public void AddHearth_SetsCorrectContainerImage()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        builder.AddHearth("ai");

        var annotation = builder.Resources
            .OfType<HearthResource>()
            .Single()
            .Annotations
            .OfType<ContainerImageAnnotation>()
            .Single();

        Assert.Equal(HearthContainerImageTags.Image, annotation.Image);
        Assert.Equal(HearthContainerImageTags.Tag, annotation.Tag);
        Assert.Equal(HearthContainerImageTags.Registry, annotation.Registry);
    }

    [Fact]
    public void AddHearth_RegistersHttpEndpoint()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        builder.AddHearth("ai");

        var endpoint = builder.Resources
            .OfType<HearthResource>()
            .Single()
            .Annotations
            .OfType<EndpointAnnotation>()
            .Single(e => e.Name == "http");

        Assert.Equal(HearthResource.DefaultPort, endpoint.TargetPort);
        Assert.Equal("http", endpoint.UriScheme);
    }

    [Fact]
    public void WithModel_AddsEnvironmentAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var resource = builder.AddHearth("ai")
            .WithModel("Qwen/Qwen2.5-7B-Instruct-GGUF");

        var envAnnotations = resource.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
        Assert.NotEmpty(envAnnotations);
    }

    [Fact]
    public async Task WithModel_SetsHearthModelEnvironmentVariable()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        builder.AddHearth("ai").WithModel("Qwen/Qwen2.5-7B-Instruct-GGUF");

        var resource = builder.Resources.OfType<HearthResource>().Single();
        var env = await GetEnvironmentVariablesAsync(resource);

        Assert.Equal("Qwen/Qwen2.5-7B-Instruct-GGUF", env["HEARTH__MODEL"]?.ToString());
    }

    [Fact]
    public async Task WithGpuAcceleration_SetsGpuLayersEnvironmentVariable()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        builder.AddHearth("ai").WithGpuAcceleration(35);

        var resource = builder.Resources.OfType<HearthResource>().Single();
        var env = await GetEnvironmentVariablesAsync(resource);

        Assert.Equal("35", env["HEARTH__GPULAYERS"]?.ToString());
    }

    [Fact]
    public async Task WithContextSize_SetsContextSizeEnvironmentVariable()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        builder.AddHearth("ai").WithContextSize(8192);

        var resource = builder.Resources.OfType<HearthResource>().Single();
        var env = await GetEnvironmentVariablesAsync(resource);

        Assert.Equal("8192", env["HEARTH__CONTEXTSIZE"]?.ToString());
    }

    [Fact]
    public void AddHearth_ThrowsOnNullBuilder()
    {
        IDistributedApplicationBuilder builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.AddHearth("ai"));
    }

    [Fact]
    public void AddHearth_ThrowsOnEmptyName()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        Assert.Throws<ArgumentException>(() => builder.AddHearth(""));
    }

    private static async Task<Dictionary<string, object>> GetEnvironmentVariablesAsync(IResource resource)
    {
        var sp = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

        var execContext = new DistributedApplicationExecutionContext(
            new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run)
            {
                ServiceProvider = sp
            });

        var envVars = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(execContext, envVars);

        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            try
            {
                await annotation.Callback(context);
            }
            catch (InvalidOperationException)
            {
                // Aspire system callbacks (e.g. OTLP exporter) require the full DI container
                // which is not available in unit-test context — skip them.
            }
        }

        return envVars;
    }
}
