using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Hearth.AspNetCore;

/// <summary>Extension methods for mapping Hearth OpenAI-compatible endpoints.</summary>
public static class HearthEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Hearth OpenAI-compatible endpoints onto the route builder:
    /// <list type="bullet">
    ///   <item><c>POST /v1/chat/completions</c> — streaming and non-streaming chat completions</item>
    ///   <item><c>POST /v1/embeddings</c> — text embeddings</item>
    ///   <item><c>GET  /v1/models</c> — lists the loaded model</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <c>IChatClient</c> and <c>IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt;</c> must be
    /// registered in the DI container before calling this method — typically via
    /// <c>services.AddHearth(…)</c> from the <c>Hearth</c> package.
    /// </remarks>
    public static IEndpointRouteBuilder MapHearth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/v1/chat/completions", ChatCompletionsEndpoint.HandleAsync);
        endpoints.MapPost("/v1/embeddings", EmbeddingsEndpoint.HandleAsync);
        endpoints.MapGet("/v1/models", ModelsEndpoint.Handle);

        return endpoints;
    }
}
