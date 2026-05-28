# Hearth vs Ollama: when to use which

Both Hearth and Ollama let you run GGUF models locally. They solve different problems. This comparison is written to be genuinely fair — we'd rather help you pick the right tool than win on paper.

## At a glance

| | Hearth | Ollama |
| --- | --- | --- |
| What it is | .NET library (NuGet package) | Standalone server + CLI |
| Installation | `dotnet add package` | Download binary / `brew install ollama` |
| Integration model | Runs inside your ASP.NET process | Runs as a separate sidecar process |
| Language | .NET only | Any language with an HTTP client |
| OpenAI-compatible API | Yes (via `Hearth.AI.AspNetCore`) | Yes |
| Model management | Manual or HuggingFace auto-download | `ollama pull <model>` |
| `IChatClient` / MEA | Native implementation | Via OpenAI-compatible wrapper |
| Embeddings | Yes | Yes |
| Tool calling | Yes | Yes |
| Streaming | Yes | Yes |
| GPU support | CUDA, Metal, Vulkan | CUDA, Metal, ROCm |
| Multi-model (hot-swap) | No (one model per process) | Yes |
| Cross-language usage | No | Yes |
| License | MIT | MIT |

## Choose Hearth when

**You're building a .NET application and want in-process inference.**

Hearth runs inside your ASP.NET Core process. There is no sidecar to deploy, no inter-process communication, no network hop for inference. If you're shipping a self-contained .NET application, Hearth is simpler to deploy and operate.

**You care about the `Microsoft.Extensions.AI` abstraction.**

`AddHearth()` registers `IChatClient` and `IEmbeddingGenerator` directly into the DI container. You can test with mock implementations, swap backends without changing business logic, and compose with middleware pipelines. Ollama's .NET SDK wraps the HTTP API; it doesn't implement the MEA interfaces natively.

**You have strict data residency requirements.**

With Hearth, inference never leaves the process. There is no local HTTP server that could theoretically be reached by another process. For environments with strict network isolation (air-gapped systems, HIPAA, etc.), in-process inference is the strongest guarantee.

**Your app ships as a single binary or container.**

Model weights + application logic + inference in one container makes deployments straightforward.

## Choose Ollama when

**You want to run one model and query it from multiple applications.**

Ollama acts as a local model server. Multiple processes — your web app, a CLI tool, a VS Code extension — can all share the same running model instance. Hearth loads the model once per process.

**You use multiple languages.**

If your stack includes Python, TypeScript, Go, or a mix, Ollama provides a common local endpoint for all of them. Hearth is .NET only.

**You need multi-model switching.**

Ollama can hot-swap between models without restarting. With Hearth, changing the model requires a process restart. For interactive developer tools or applications where users choose different models on the fly, Ollama is better suited.

**You want the `ollama` CLI experience.**

`ollama pull`, `ollama run`, `ollama list` — if you want a polished model management CLI, Ollama ships one. Hearth has no CLI beyond `dotnet run`.

**You want ROCm (AMD) GPU support.**

Hearth currently supports CUDA, Metal, and Vulkan backends. Ollama also supports ROCm, which covers older AMD GPU architectures that don't support Vulkan compute.

## Using both together

They're not mutually exclusive. A common pattern:

- Developers use **Ollama** locally for quick experiments across multiple tools.
- The deployed application uses **Hearth** for in-process, containerized inference.

Your app can be designed to support both with a single configuration toggle:

```csharp
if (builder.Configuration["Inference:Mode"] == "local-in-process")
{
    builder.Services.AddHearth(o => o.Model = builder.Configuration["Hearth:Model"]!);
}
else
{
    // Point the OpenAI client at the Ollama endpoint
    builder.Services.AddOpenAIClient(o =>
        o.Endpoint = new Uri(builder.Configuration["Ollama:BaseUrl"]!));
}
```

## Summary

| Scenario | Recommended |
| --- | --- |
| .NET-only app, single deployment unit | Hearth |
| Multi-language stack | Ollama |
| Shared local model across multiple processes | Ollama |
| In-process, zero-sidecar production deployment | Hearth |
| Multi-model switching per request | Ollama |
| Strict air-gap / data residency in .NET | Hearth |
| Developer workflow with many tools | Ollama |
