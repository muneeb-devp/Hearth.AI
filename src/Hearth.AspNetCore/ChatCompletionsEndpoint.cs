using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;

namespace Hearth.AspNetCore;

internal static class ChatCompletionsEndpoint
{
    internal static async Task HandleAsync(
        OpenAiChatRequest request,
        IChatClient chatClient,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request.Messages is null || request.Messages.Count == 0)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = new { message = "messages is required", type = "invalid_request_error" } },
                cancellationToken);
            return;
        }

        var messages = MapMessages(request.Messages);
        var options = new ChatOptions
        {
            Temperature = request.Temperature,
            MaxOutputTokens = request.MaxTokens,
        };

        var modelId = request.Model ?? "hearth-local";
        var completionId = $"chatcmpl-{Guid.NewGuid():N}";

        if (request.Stream)
        {
            await StreamAsync(httpContext, chatClient, messages, options, modelId, completionId, cancellationToken);
        }
        else
        {
            await RespondAsync(httpContext, chatClient, messages, options, modelId, completionId, cancellationToken);
        }
    }

    private static async Task RespondAsync(
        HttpContext httpContext,
        IChatClient chatClient,
        List<Microsoft.Extensions.AI.ChatMessage> messages,
        ChatOptions options,
        string modelId,
        string completionId,
        CancellationToken cancellationToken)
    {
        var chatResponse = await chatClient.GetResponseAsync(messages, options, cancellationToken);
        var text = chatResponse.Messages.Count > 0 ? chatResponse.Messages[0].Text ?? string.Empty : string.Empty;

        var response = new OpenAiChatResponse
        {
            Id = completionId,
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = modelId,
            Choices =
            [
                new OpenAiChoice
                {
                    Index = 0,
                    Message = new OpenAiMessage { Role = "assistant", Content = text },
                    FinishReason = "stop",
                }
            ],
        };

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    }

    private static async Task StreamAsync(
        HttpContext httpContext,
        IChatClient chatClient,
        List<Microsoft.Extensions.AI.ChatMessage> messages,
        ChatOptions options,
        string modelId,
        string completionId,
        CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "text/event-stream; charset=utf-8";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no";

        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await WriteSseChunkAsync(httpContext.Response, new OpenAiChatChunk
        {
            Id = completionId,
            Created = created,
            Model = modelId,
            Choices = [new OpenAiChunkChoice { Delta = new OpenAiDelta { Role = "assistant" }, FinishReason = null }],
        }, cancellationToken);

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            var token = update.Text;
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }

            await WriteSseChunkAsync(httpContext.Response, new OpenAiChatChunk
            {
                Id = completionId,
                Created = created,
                Model = modelId,
                Choices = [new OpenAiChunkChoice { Delta = new OpenAiDelta { Content = token }, FinishReason = null }],
            }, cancellationToken);
        }

        await WriteSseChunkAsync(httpContext.Response, new OpenAiChatChunk
        {
            Id = completionId,
            Created = created,
            Model = modelId,
            Choices = [new OpenAiChunkChoice { Delta = new OpenAiDelta(), FinishReason = "stop" }],
        }, cancellationToken);

        await httpContext.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    private static async Task WriteSseChunkAsync(HttpResponse response, OpenAiChatChunk chunk, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(chunk);
        await response.WriteAsync($"data: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }

    internal static List<Microsoft.Extensions.AI.ChatMessage> MapMessages(List<OpenAiMessage> messages) =>
        messages.Select(static m => new Microsoft.Extensions.AI.ChatMessage(
            m.Role switch
            {
                "system" => ChatRole.System,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User,
            },
            m.Content ?? string.Empty))
        .ToList();
}
