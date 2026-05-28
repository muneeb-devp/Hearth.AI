# Hearth documentation

Hearth brings local GGUF inference to the standard .NET AI abstractions, so you can register a model once and keep the rest of your app on `IChatClient` and `IEmbeddingGenerator`.

## Start here

| Need | Page |
| --- | --- |
| Install the package and run your first prompt | [Getting started](articles/getting-started.md) |
| Understand `HearthOptions` and model selection | [Configuration](articles/configuration.md) |
| Expose OpenAI-compatible endpoints in ASP.NET Core | [ASP.NET Core integration](articles/aspnetcore.md) |
| Pick the right GPU backend package | [GPU backends](articles/gpu-backends.md) |
| Explore the sample apps in this repo | [Samples](articles/samples.md) |
| Browse the public API surface | [API reference](api/index.md) |
| Swap an existing OpenAI app to local inference | [Replacing OpenAI with Hearth](articles/replacing-openai.md) |
| Pick the right model and quantization for your hardware | [Choosing the right model](articles/model-selection.md) |
| Define and invoke tools from .NET methods | [Tool calling](articles/tool-calling.md) |
| Docker, Kubernetes, performance tuning | [Deploying to production](articles/production.md) |
| Understand the trade-offs vs Ollama | [Hearth vs Ollama](articles/hearth-vs-ollama.md) |

## What Hearth includes

- **Single-line registration** with `AddHearth(...)`
- **Local chat and embeddings** over `Microsoft.Extensions.AI`
- **OpenAI-compatible endpoints** via `MapHearth()`
- **Optional GPU backends** for CUDA, Metal, and Vulkan
- **Console and Blazor samples** for local experimentation

## Core registration example

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
    options.ContextSize = 8192;
    options.GpuLayers = 35;
});
```

Once registered, inject `IChatClient` anywhere in your app and keep the rest of your code independent from the model host.
