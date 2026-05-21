using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Hearth;

/// <summary>
/// <see cref="IChatClient"/> backed by a local GGUF model via LLamaSharp.
/// Each <see cref="GetResponseAsync"/> or <see cref="GetStreamingResponseAsync"/> call
/// allocates a fresh LLamaContext, so calls are independent and thread-safe w.r.t. state.
/// </summary>
internal sealed class HearthChatClient : IChatClient
{
    private const int MaxToolRounds = 5;

    private readonly HearthModel _model;
    private readonly ILogger<HearthChatClient> _logger;
    private readonly ChatTemplateFamily _family;

    /// <inheritdoc />
    public ChatClientMetadata Metadata { get; }

    internal HearthChatClient(HearthModel model, ILogger<HearthChatClient> logger)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(logger);
        _model = model;
        _logger = logger;
        _family = ChatTemplate.DetectFamily(model.ModelPath);
        Metadata = new ChatClientMetadata(
            providerName: "Hearth",
            providerUri: null,
            defaultModelId: Path.GetFileNameWithoutExtension(model.ModelPath));
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);

        var messages = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();
        Log.StartingInference(_logger, messages.Count);

        var tools = options?.Tools?.OfType<AIFunction>().ToList();
        var hasTools = tools is { Count: > 0 }
            && !ReferenceEquals(options?.ToolMode, ChatToolMode.None);

        var history = hasTools
            ? InjectToolContext(messages, tools!)
            : new List<ChatMessage>(messages);

        for (var round = 0; round < (hasTools ? MaxToolRounds : 1); round++)
        {
            var prompt = ChatTemplate.Format(history, _family);
            var executor = _model.CreateExecutor();
            var inferenceParams = BuildInferenceParams(options);
            var sb = new StringBuilder();

            try
            {
                await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken).ConfigureAwait(false))
                {
                    sb.Append(token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.InferenceFailed(_logger, sb.Length, ex);
                throw;
            }

            var responseText = sb.ToString().Trim();

            if (hasTools && ToolCallParser.TryParse(responseText, out var toolName, out var toolArgs))
            {
                var func = tools!.FirstOrDefault(t =>
                    string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));

                if (func is not null)
                {
                    Log.ToolCallStarted(_logger, func.Name);

                    var callId = Guid.NewGuid().ToString("N")[..8];
                    history.Add(new ChatMessage(ChatRole.Assistant, responseText));

                    try
                    {
                        var funcArgs = toolArgs is not null
                            ? new AIFunctionArguments(toolArgs!)
                            : new AIFunctionArguments();

                        var result = await func.InvokeAsync(funcArgs, cancellationToken).ConfigureAwait(false);
                        var resultJson = result is not null
                            ? JsonSerializer.Serialize(result)
                            : "null";

                        history.Add(new ChatMessage(new ChatRole("tool"),
                            $"Tool {func.Name} returned: {resultJson}"));

                        Log.ToolCallCompleted(_logger, func.Name);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log.ToolCallFailed(_logger, func.Name, ex);
                        history.Add(new ChatMessage(new ChatRole("tool"),
                            $"Tool {func.Name} failed: {ex.Message}"));
                    }

                    continue;
                }
            }

            Log.InferenceCompleted(_logger, sb.Length);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText));
        }

        Log.InferenceCompleted(_logger, 0);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);
        var messages = chatMessages as IList<ChatMessage> ?? chatMessages.ToList();

        Log.StartingInference(_logger, messages.Count);

        var prompt = ChatTemplate.Format(messages, _family);
        var executor = _model.CreateExecutor();
        var inferenceParams = BuildInferenceParams(options);
        var tokenCount = 0;

        await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken).ConfigureAwait(false))
        {
            tokenCount++;
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent(token)],
            };
        }

        Log.InferenceCompleted(_logger, tokenCount);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null)
        {
            if (serviceType.IsInstanceOfType(this))
            {
                return this;
            }

            if (serviceType == typeof(ChatClientMetadata))
            {
                return Metadata;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose() { }

    private InferenceParams BuildInferenceParams(ChatOptions? options) => new()
    {
        MaxTokens = options?.MaxOutputTokens ?? 2048,
        AntiPrompts = ChatTemplate.GetAntiPrompts(_family),
        SamplingPipeline = new DefaultSamplingPipeline
        {
            Temperature = (float)(options?.Temperature ?? 0.7),
        },
    };

    private static List<ChatMessage> InjectToolContext(IList<ChatMessage> messages, IList<AIFunction> tools)
    {
        var toolDesc = BuildToolDescription(tools);
        var history = new List<ChatMessage>(messages.Count + 1);

        if (messages.Count > 0 && messages[0].Role == ChatRole.System)
        {
            history.Add(new ChatMessage(ChatRole.System,
                (messages[0].Text ?? string.Empty) + "\n\n" + toolDesc));

            for (var i = 1; i < messages.Count; i++)
            {
                history.Add(messages[i]);
            }
        }
        else
        {
            history.Add(new ChatMessage(ChatRole.System, toolDesc));
            history.AddRange(messages);
        }

        return history;
    }

    private static string BuildToolDescription(IList<AIFunction> tools)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You have access to the following tools. To use a tool, respond with ONLY this JSON and nothing else:");
        sb.AppendLine("{\"tool_call\": {\"name\": \"FUNCTION_NAME\", \"arguments\": {\"PARAM\": VALUE}}}");
        sb.AppendLine();
        sb.AppendLine("Available tools:");
        sb.Append('[');

        for (var i = 0; i < tools.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            var tool = tools[i];
            sb.Append($"{{\"name\": \"{EscapeJson(tool.Name)}\", ");
            sb.Append($"\"description\": \"{EscapeJson(tool.Description ?? string.Empty)}\", ");
            sb.Append($"\"parameters\": {JsonSerializer.Serialize(tool.JsonSchema)}}}");
        }

        sb.AppendLine("]");
        sb.AppendLine();
        sb.Append("If you don't need a tool, respond normally without the tool_call JSON.");
        return sb.ToString();
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("\"", "\\\"", StringComparison.Ordinal);
}
