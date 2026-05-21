# Hearth

> Batteries-included local LLM inference for .NET

![Hearth icon](hearth-icon.svg)

[![NuGet](https://img.shields.io/nuget/v/Hearth.AI.svg)](https://www.nuget.org/packages/Hearth.AI)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/muneeb-devp/Hearth.AI/ci.yml?branch=main)](https://github.com/muneeb-devp/Hearth.AI/actions)

Run any GGUF language model locally in a .NET 8/9 app with a **single line of registration**:

```csharp
builder.Services.AddHearth(o => o.Model = "./models/qwen2.5-7b-q4_k_m.gguf");
```

Hearth implements the standard `IChatClient` interface from `Microsoft.Extensions.AI`, so your application code is identical whether the inference backend is a local model or a cloud API. Swap models without changing a line of business logic.

---

## Why Hearth?

The .NET AI ecosystem offers many cloud-hosted model clients but very little for **on-device, private inference**. Existing options require significant LLamaSharp boilerplate, don't integrate with the `Microsoft.Extensions.*` stack, or lack streaming support.

Hearth solves that:

| Problem                         | How Hearth helps                                              |
| ------------------------------- | ------------------------------------------------------------- |
| LLamaSharp setup is verbose     | One `AddHearth()` call wires everything up                    |
| Cloud APIs leak sensitive data  | All inference runs on your machine                            |
| Vendor lock-in                  | Implements `IChatClient` — swap backends without code changes |
| GGUF model variety is confusing | Sane defaults; ChatML template covers 90% of modern models    |
| Streaming is hard to wire up    | `GetStreamingResponseAsync` works out of the box              |

---

## Use Cases

**Privacy-sensitive workloads** — Legal document review, medical triage, HR workflows: data never leaves your infrastructure.

**Air-gapped environments** — Industrial control systems, secure government networks, or any deployment without internet access.

**Cost control at scale** — Running millions of inference calls against a cloud API gets expensive fast. A local model on modest hardware is a fixed cost.

**Developer tooling and CI** — Code review bots, test fixture generators, commit message writers — run them in CI without API keys or rate limits.

**Edge and embedded** — Raspberry Pi 5, Jetson Nano, or in-vehicle compute where latency to a data centre is unacceptable.

**Compliance and data residency** — GDPR, HIPAA, or contractual requirements that prohibit sending data to third-party processors.

---

## Quick Start

### 1. Install

```
dotnet add package Hearth.AI
```

### 2. Download a GGUF model

Hearth works with any ChatML-compatible GGUF. [Qwen 2.5](https://huggingface.co/Qwen/Qwen2.5-7B-Instruct-GGUF) is an excellent starting point:

```bash
# ~4 GB — good quality-to-size ratio
wget https://huggingface.co/Qwen/Qwen2.5-7B-Instruct-GGUF/resolve/main/qwen2.5-7b-instruct-q4_k_m.gguf
```

### 3. Register and inject

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
    options.ContextSize = 8192;
    options.GpuLayers = 35;  // 0 = CPU-only, 999 = offload everything
});
```

Then inject `IChatClient` anywhere in your application:

```csharp
public class SummaryService(IChatClient chat)
{
    public async Task<string> SummarizeAsync(string document, CancellationToken ct = default)
    {
        var response = await chat.GetResponseAsync(
        [
            new(ChatRole.System, "Summarize the following document in three sentences."),
            new(ChatRole.User, document)
        ], cancellationToken: ct);

        return response.Message.Text ?? string.Empty;
    }
}
```

---

## Code Examples

### Single-turn question answering

```csharp
var response = await chat.GetResponseAsync(
[
    new(ChatRole.System, "You are a helpful assistant. Be concise."),
    new(ChatRole.User, "What is the capital of Japan?")
]);

Console.WriteLine(response.Message.Text); // "Tokyo."
```

### Streaming response

```csharp
await foreach (var update in chat.GetStreamingResponseAsync(
[
    new(ChatRole.User, "Write a haiku about autumn.")
]))
{
    Console.Write(update.Text);  // tokens arrive as they are generated
}
```

### Multi-turn conversation with history

```csharp
var history = new List<ChatMessage>
{
    new(ChatRole.System, "You are a knowledgeable but concise assistant.")
};

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null || input == "exit") break;

    history.Add(new(ChatRole.User, input));

    var sb = new StringBuilder();
    await foreach (var update in chat.GetStreamingResponseAsync(history))
    {
        Console.Write(update.Text);
        sb.Append(update.Text);
    }
    Console.WriteLine();

    history.Add(new(ChatRole.Assistant, sb.ToString().Trim()));
}
```

### Document classification

```csharp
public enum Sentiment { Positive, Negative, Neutral }

public async Task<Sentiment> ClassifyAsync(string review)
{
    var response = await chat.GetResponseAsync(
    [
        new(ChatRole.System,
            "Classify the sentiment of the review as exactly one word: Positive, Negative, or Neutral."),
        new(ChatRole.User, review)
    ]);

    return Enum.TryParse<Sentiment>(response.Message.Text?.Trim(), ignoreCase: true, out var result)
        ? result
        : Sentiment.Neutral;
}
```

### Structured JSON extraction

```csharp
public record PersonInfo(string Name, int Age, string City);

public async Task<PersonInfo?> ExtractAsync(string paragraph)
{
    var response = await chat.GetResponseAsync(
    [
        new(ChatRole.System,
            """
            Extract person information from the text.
            Respond with valid JSON only, no explanation.
            Schema: {"Name": string, "Age": number, "City": string}
            """),
        new(ChatRole.User, paragraph)
    ]);

    var json = response.Message.Text?.Trim();
    return json is null ? null : JsonSerializer.Deserialize<PersonInfo>(json);
}
```

### ASP.NET Core minimal API endpoint

```csharp
builder.Services.AddHearth(o => o.Model = "./models/qwen2.5-7b-q4_k_m.gguf");

var app = builder.Build();

app.MapPost("/ask", async (AskRequest req, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(
    [
        new(ChatRole.System, "You are a helpful assistant."),
        new(ChatRole.User, req.Question)
    ]);
    return Results.Ok(new { Answer = response.Message.Text });
});

record AskRequest(string Question);
```

### Background worker with cancellation

```csharp
public class DocumentProcessorWorker(IChatClient chat) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var doc in documentQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                var response = await chat.GetResponseAsync(
                [
                    new(ChatRole.System, "Summarize in one paragraph."),
                    new(ChatRole.User, doc.Content)
                ], cancellationToken: stoppingToken);

                await SaveSummaryAsync(doc.Id, response.Message.Text ?? string.Empty);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
```

### `ChatOptions` for per-call temperature control

```csharp
var creative = new ChatOptions { Temperature = 1.2f, MaxOutputTokens = 512 };
var focused  = new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 128 };

var story = await chat.GetResponseAsync(storyMessages, creative);
var fact  = await chat.GetResponseAsync(factMessages,  focused);
```

---

## Configuration Reference

Register options via `AddHearth(Action<HearthOptions>)`:

```csharp
builder.Services.AddHearth(options =>
{
    options.Model        = "./models/qwen2.5-7b-q4_k_m.gguf";
    options.ContextSize  = 8192;
    options.GpuLayers    = 35;
    options.BatchSize    = 512;
    options.Threads      = -1;
});
```

| Property         | Type      | Default            | Description                                                                                                                            |
| ---------------- | --------- | ------------------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| `Model`          | `string?` | _(required)_       | Path to a local `.gguf` file, or a Hugging Face repo ID (e.g. `Qwen/Qwen2.5-7B-Instruct-GGUF`).                                        |
| `ModelFile`      | `string?` | `null`             | Specific file within a `CacheDirectory`. Ignored when `Model` is already a direct path.                                                |
| `ContextSize`    | `int`     | `4096`             | Maximum tokens in the context window (prompt + response). Larger values need more RAM/VRAM.                                            |
| `GpuLayers`      | `int`     | `0`                | Layers to offload to GPU. `0` = CPU-only. `999` = offload all layers. A good starting point for a 7B Q4 model on an 8 GB card is `35`. |
| `BatchSize`      | `int`     | `512`              | Prompt-processing batch size. Higher values improve throughput on long prompts at the cost of memory.                                  |
| `Threads`        | `int`     | `-1`               | CPU threads for inference. `-1` = let the runtime decide based on available cores.                                                     |
| `CacheDirectory` | `string?` | `~/.hearth/models` | Directory for cached model files (used in Phase 2 auto-download).                                                                      |

### Binding from `appsettings.json`

```json
{
  "Hearth": {
    "Model": "./models/qwen2.5-7b-q4_k_m.gguf",
    "ContextSize": 8192,
    "GpuLayers": 35
  }
}
```

```csharp
builder.Services.AddHearth(options =>
    builder.Configuration.GetSection("Hearth").Bind(options));
```

---

## Supported Models

Hearth uses the **ChatML** prompt template, which covers the majority of modern instruction-tuned GGUFs:

| Model family           | Example GGUF repo                            | Notes             |
| ---------------------- | -------------------------------------------- | ----------------- |
| Qwen 2.5               | `Qwen/Qwen2.5-7B-Instruct-GGUF`              | Native ChatML     |
| Llama 3 / 3.1 / 3.2    | `bartowski/Meta-Llama-3-8B-Instruct-GGUF`    | Native ChatML     |
| Mistral v0.3 / Mixtral | `TheBloke/Mistral-7B-Instruct-v0.3-GGUF`     | ChatML compatible |
| Phi-3 / Phi-3.5        | `microsoft/Phi-3-mini-4k-instruct-gguf`      | ChatML compatible |
| Gemma 2                | `bartowski/gemma-2-9b-it-GGUF`               | ChatML compatible |
| DeepSeek R1            | `bartowski/DeepSeek-R1-Distill-Qwen-7B-GGUF` | Native ChatML     |

**Recommended quantizations:**

- `Q4_K_M` — best balance of size and quality; the default choice
- `Q5_K_M` / `Q6_K` — higher quality if RAM/VRAM allows
- `Q8_0` — near-lossless; only practical on large-memory machines
- Avoid `Q2_K` for tasks requiring coherent multi-step reasoning

Hearth auto-detects the template family from the GGUF filename and applies the correct prompt format — no manual configuration required.

---

## Performance Guide

### GPU layers

GPU offload is the single biggest performance lever. Even partial offload gives a large speedup:

```csharp
// Apple Silicon — offload everything; Metal backend handles it
options.GpuLayers = 999;

// NVIDIA 8 GB + 7B Q4 model (~4 GB)
// Start at 35 and raise until VRAM runs out
options.GpuLayers = 35;

// CPU-only (default)
options.GpuLayers = 0;
```

GPU backend packages that unlock `GpuLayers > 0` are planned for Phase 5:

| Package            | Backend                        |
| ------------------ | ------------------------------ |
| `Hearth.AI`        | CPU (llama.cpp AVX2) — default |
| `Hearth.AI.Cuda`   | NVIDIA CUDA 12                 |
| `Hearth.AI.Metal`  | Apple Metal (M-series)         |
| `Hearth.AI.Vulkan` | Vulkan — AMD/Intel GPUs        |

### Memory usage

| Factor              | Rule of thumb                                        |
| ------------------- | ---------------------------------------------------- |
| `ContextSize`       | Each extra 1024 tokens ≈ +0.5 GB VRAM for a 7B model |
| Concurrent requests | Each in-flight request holds its own KV-cache        |
| Model size          | 7B Q4_K_M ≈ 4 GB; 13B Q4_K_M ≈ 8 GB                  |

For batch/background processing (not real-time), lower `ContextSize` to 2048 if your prompts are short. For a long-context use case (document Q&A), 16 384 or 32 768 may be worth the memory cost.

### CPU threads

```csharp
// Cap to half the physical cores when running alongside other services
options.Threads = Environment.ProcessorCount / 2;
```

`-1` (auto) works well for single-process deployments.

---

## Architecture

```
Your application code
        │
        │  IChatClient  (Microsoft.Extensions.AI)
        ▼
HearthChatClient
        │  formats prompt via ChatML template
        │  builds InferenceParams from ChatOptions
        ▼
StatelessExecutor  (LLamaSharp)
        │  allocates a fresh KV-cache per call
        │  streams tokens back as IAsyncEnumerable<string>
        ▼
LLamaWeights  (singleton, loaded once at DI resolution)
        │
        ▼
llama.cpp  (native GGUF inference)
```

**Singleton model, stateless executor.** `LLamaWeights` is loaded once and held for the application lifetime. Each `GetResponseAsync` or `GetStreamingResponseAsync` call creates a short-lived `StatelessExecutor` with its own KV-cache, which is freed when the call completes. Consequences:

- Concurrent calls are safe — no shared mutable state between requests.
- No session lifetime to manage.
- Memory per concurrent request ≈ `ContextSize × num_layers × sizeof(float16)`.

**Lazy loading.** The model loads on first DI resolution, not at `builder.Build()`. Startup time is unaffected; the first inference call pays the load cost (typically 1–5 seconds depending on model size and disk speed).

---

## Console Sample

The repo includes a streaming chat REPL in `samples/Hearth.Samples.Console`:

```bash
# Pass model path as CLI argument
dotnet run --project samples/Hearth.Samples.Console -- ./models/qwen2.5-7b-q4_k_m.gguf

# Or via environment variable
HEARTH_MODEL=./models/qwen2.5-7b-q4_k_m.gguf dotnet run --project samples/Hearth.Samples.Console
```

Sample session:

```
╔══════════════════════════════╗
║        Hearth Chat           ║
╚══════════════════════════════╝
Model : /home/user/models/qwen2.5-7b-q4_k_m.gguf
Quit  : type 'exit' or press Ctrl+C

> What is a KV-cache in the context of transformer inference?
Assistant: A KV-cache stores the key/value attention tensors for tokens already processed...
```

Type `exit` or press `Ctrl+C` to quit.

---

## Blazor Sample

`samples/Hearth.Samples.Blazor` is a Blazor Server streaming chat UI. Tokens appear in the browser as they are generated — no polling, no page reloads.

```bash
dotnet run --project samples/Hearth.Samples.Blazor -- ./models/qwen2.5-7b-q4_k_m.gguf
```

Then open `http://localhost:5000` in a browser. The app uses `IChatClient.GetStreamingResponseAsync` with `StateHasChanged()` calls on each token so the UI updates in real time over the existing SignalR connection.

---

## Roadmap

| Phase | Feature                                                                                                                                      | Status  |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| 1     | Local GGUF inference, `IChatClient`, streaming, console sample                                                                               | ✅ Done |
| 2     | Hugging Face model downloader — resumable, SHA-verified, progress callbacks; auto-quantization selection; lazy model loading                 | ✅ Done |
| 3     | Tool/function calling; `IEmbeddingGenerator<string, Embedding<float>>`; per-model-family chat templates                                      | ✅ Done |
| 4     | `Hearth.AspNetCore` — `MapHearth()` extension; OpenAI-compatible `/v1/chat/completions`, `/v1/embeddings`, `/v1/models`; SSE streaming       | ✅ Done |
| 5     | Blazor streaming chat sample; GitHub Actions CI/CD; NuGet 1.0 release; GPU backend packages (`Hearth.Cuda`, `Hearth.Metal`, `Hearth.Vulkan`) | ✅ Done |

---

## Requirements

- **.NET 8** or **.NET 9**
- A `.gguf` model file — ChatML-compatible models are recommended (see [Supported Models](#supported-models))
- No GPU required for CPU-only inference

### Platform support

Hearth inherits LLamaSharp's platform matrix:

| Platform | Architecture       | Notes                |
| -------- | ------------------ | -------------------- |
| Windows  | x64, arm64         | Full support         |
| Linux    | x64, arm64         | Full support         |
| macOS    | x64, Apple Silicon | Metal GPU in Phase 5 |

---

## Author

**Muneeb Mughal**

- GitHub: [github.com/muneeb-devp](https://github.com/muneeb-devp)
- LinkedIn: [linkedin.com/in/muneeb-mughal-](https://linkedin.com/in/muneeb-mughal-)
- Email: muneeb.devp@gmail.com

---

## Contributing

Contributions are welcome. Please open an issue before submitting a large PR so the approach can be discussed.

```bash
# Clone and build
git clone https://github.com/muneeb-devp/Hearth.git
cd Hearth
dotnet build

# Run all tests
dotnet test

# Run the console sample (requires a local GGUF)
dotnet run --project samples/Hearth.Samples.Console -- /path/to/model.gguf
```

Code style is enforced by `.editorconfig` and `EnforceCodeStyleInBuild=true`. CI will reject PRs with style warnings.

---

## License

[MIT](LICENSE)
