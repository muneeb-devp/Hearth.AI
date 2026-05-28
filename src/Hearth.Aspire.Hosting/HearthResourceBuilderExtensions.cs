using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Hearth.Aspire.Hosting;

/// <summary>Extension methods for adding Hearth inference server resources to an Aspire application.</summary>
public static class HearthResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Hearth inference server container to the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Resource name — used as the connection-string key in consuming projects.</param>
    /// <returns>A builder for further configuration of the Hearth resource.</returns>
    public static IResourceBuilder<HearthResource> AddHearth(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var resource = new HearthResource(name);
        return builder
            .AddResource(resource)
            .WithImage(HearthContainerImageTags.Image, HearthContainerImageTags.Tag)
            .WithImageRegistry(HearthContainerImageTags.Registry)
            .WithHttpEndpoint(port: null, targetPort: HearthResource.DefaultPort, name: "http")
            .WithOtlpExporter();
    }

    /// <summary>Sets the model to load. Accepts a Hugging Face repo ID (e.g. <c>Qwen/Qwen2.5-7B-Instruct-GGUF</c>) or a local path inside the container.</summary>
    public static IResourceBuilder<HearthResource> WithModel(
        this IResourceBuilder<HearthResource> builder,
        string modelOrRepoId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelOrRepoId);
        return builder.WithEnvironment("HEARTH__MODEL", modelOrRepoId);
    }

    /// <summary>Offloads model layers to GPU. Pass the number of layers or <c>999</c> to offload all.</summary>
    public static IResourceBuilder<HearthResource> WithGpuAcceleration(
        this IResourceBuilder<HearthResource> builder,
        int layers = 999)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("HEARTH__GPULAYERS", layers.ToString());
    }

    /// <summary>Sets the context window size in tokens.</summary>
    public static IResourceBuilder<HearthResource> WithContextSize(
        this IResourceBuilder<HearthResource> builder,
        int tokens)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("HEARTH__CONTEXTSIZE", tokens.ToString());
    }

    /// <summary>Binds a host directory as the model cache volume (<c>/app/models</c> inside the container).</summary>
    public static IResourceBuilder<HearthResource> WithModelCacheMount(
        this IResourceBuilder<HearthResource> builder,
        string hostPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        return builder.WithBindMount(hostPath, "/app/models");
    }

    /// <summary>Provides a Hugging Face access token for gated or private model repositories.</summary>
    public static IResourceBuilder<HearthResource> WithHuggingFaceToken(
        this IResourceBuilder<HearthResource> builder,
        IResourceBuilder<ParameterResource> token)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(token);
        return builder.WithEnvironment("HEARTH__HUGGINGFACETOKEN", token);
    }
}
