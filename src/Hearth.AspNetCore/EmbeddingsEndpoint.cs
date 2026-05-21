using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;

namespace Hearth.AspNetCore;

internal static class EmbeddingsEndpoint
{
    internal static async Task HandleAsync(
        OpenAiEmbeddingRequest request,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        List<string> inputs;

        try
        {
            inputs = ParseInputs(request.Input);
        }
        catch (ArgumentException ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = new { message = ex.Message, type = "invalid_request_error" } },
                cancellationToken);
            return;
        }

        if (inputs.Count == 0)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = new { message = "input must not be empty", type = "invalid_request_error" } },
                cancellationToken);
            return;
        }

        var modelId = request.Model ?? "hearth-local";
        var generated = await embeddingGenerator.GenerateAsync(inputs, cancellationToken: cancellationToken);

        var data = generated
            .Select((emb, i) => new OpenAiEmbeddingData
            {
                Index = i,
                Embedding = emb.Vector.ToArray(),
            })
            .ToList();

        var response = new OpenAiEmbeddingResponse
        {
            Model = modelId,
            Data = data,
        };

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    }

    internal static List<string> ParseInputs(JsonElement input) =>
        input.ValueKind switch
        {
            JsonValueKind.String => [input.GetString()!],
            JsonValueKind.Array => input.EnumerateArray()
                .Select(static e => e.GetString() ?? string.Empty)
                .ToList(),
            _ => throw new ArgumentException("input must be a string or array of strings"),
        };
}
