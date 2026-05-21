using LLama;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hearth;

/// <summary>Extension methods for registering Hearth services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Hearth local LLM inference, registering <see cref="IChatClient"/> and
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> backed by a local GGUF model
    /// loaded via LLamaSharp. The model is loaded on first use.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">Action to configure <see cref="HearthOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddHearth(options =>
    /// {
    ///     options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
    ///     options.ContextSize = 8192;
    ///     options.GpuLayers = 35;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddHearth(
        this IServiceCollection services,
        Action<HearthOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        services.AddSingleton<HearthModel>(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<HearthOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<HearthModel>>();
            return HearthModel.Load(options, logger);
        });

        services.AddSingleton<IChatClient>(static sp =>
        {
            var model = sp.GetRequiredService<HearthModel>();
            var logger = sp.GetRequiredService<ILogger<HearthChatClient>>();
            return new HearthChatClient(model, logger);
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(static sp =>
        {
            var model = sp.GetRequiredService<HearthModel>();
            var logger = sp.GetRequiredService<ILogger<HearthModel>>();
            return new LLamaEmbedder(model.Weights, model.CreateEmbeddingParams(), logger);
        });

        return services;
    }
}
