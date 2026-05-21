using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hearth.AspNetCore;

/// <summary>OpenAI-compatible <c>POST /v1/chat/completions</c> request body.</summary>
public sealed class OpenAiChatRequest
{
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("messages")] public List<OpenAiMessage>? Messages { get; set; }
    [JsonPropertyName("temperature")] public float? Temperature { get; set; }
    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }
    [JsonPropertyName("stream")] public bool Stream { get; set; }
}

/// <summary>A single chat message in the OpenAI message format.</summary>
public sealed class OpenAiMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
    [JsonPropertyName("content")] public string? Content { get; set; }
}

/// <summary>OpenAI-compatible <c>POST /v1/chat/completions</c> non-streaming response.</summary>
public sealed class OpenAiChatResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("object")] public string Object { get; set; } = "chat.completion";
    [JsonPropertyName("created")] public long Created { get; set; }
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("choices")] public List<OpenAiChoice> Choices { get; set; } = [];
    [JsonPropertyName("usage")] public OpenAiUsage Usage { get; set; } = new();
}

/// <summary>A single choice in a chat completion response.</summary>
public sealed class OpenAiChoice
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("message")] public OpenAiMessage Message { get; set; } = new();
    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = "stop";
}

/// <summary>Token usage counters in a chat completion response.</summary>
public sealed class OpenAiUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}

/// <summary>A single SSE chunk emitted during streaming chat completions.</summary>
public sealed class OpenAiChatChunk
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("object")] public string Object { get; set; } = "chat.completion.chunk";
    [JsonPropertyName("created")] public long Created { get; set; }
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("choices")] public List<OpenAiChunkChoice> Choices { get; set; } = [];
}

/// <summary>A single choice delta inside a streaming chunk.</summary>
public sealed class OpenAiChunkChoice
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("delta")] public OpenAiDelta Delta { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FinishReason { get; set; }
}

/// <summary>The incremental delta carried by a streaming chunk.</summary>
public sealed class OpenAiDelta
{
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }
}

/// <summary>OpenAI-compatible <c>POST /v1/embeddings</c> request body.</summary>
public sealed class OpenAiEmbeddingRequest
{
    [JsonPropertyName("model")] public string? Model { get; set; }

    /// <summary>A string or array of strings to embed.</summary>
    [JsonPropertyName("input")] public JsonElement Input { get; set; }

    [JsonPropertyName("encoding_format")] public string? EncodingFormat { get; set; }
}

/// <summary>OpenAI-compatible <c>POST /v1/embeddings</c> response.</summary>
public sealed class OpenAiEmbeddingResponse
{
    [JsonPropertyName("object")] public string Object { get; set; } = "list";
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("data")] public List<OpenAiEmbeddingData> Data { get; set; } = [];
    [JsonPropertyName("usage")] public OpenAiEmbeddingUsage Usage { get; set; } = new();
}

/// <summary>A single embedding vector in the response.</summary>
public sealed class OpenAiEmbeddingData
{
    [JsonPropertyName("object")] public string Object { get; set; } = "embedding";
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("embedding")] public float[] Embedding { get; set; } = [];
}

/// <summary>Token usage counters in an embeddings response.</summary>
public sealed class OpenAiEmbeddingUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}

/// <summary>OpenAI-compatible <c>GET /v1/models</c> response.</summary>
public sealed class OpenAiModelsListResponse
{
    [JsonPropertyName("object")] public string Object { get; set; } = "list";
    [JsonPropertyName("data")] public List<OpenAiModelInfo> Data { get; set; } = [];
}

/// <summary>Metadata for a single model returned by <c>GET /v1/models</c>.</summary>
public sealed class OpenAiModelInfo
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("object")] public string Object { get; set; } = "model";
    [JsonPropertyName("created")] public long Created { get; set; }
    [JsonPropertyName("owned_by")] public string OwnedBy { get; set; } = "hearth";
}
