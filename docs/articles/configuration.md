# Configuration

Hearth is configured through `AddHearth(Action<HearthOptions>)`.

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
    options.ContextSize = 8192;
    options.GpuLayers = 35;
    options.BatchSize = 512;
    options.Threads = -1;
});
```

## Key options

| Option | Default | What it controls |
| --- | --- | --- |
| `Model` | required | Local `.gguf` path or a Hugging Face repo ID |
| `ModelFile` | `null` | Exact model file to use from a cached repo |
| `ContextSize` | `4096` | Prompt + response context window |
| `GpuLayers` | `0` | Layers offloaded to the GPU |
| `BatchSize` | `512` | Prompt-processing throughput vs memory tradeoff |
| `Threads` | `-1` | CPU thread count for inference |
| `CacheDirectory` | `~/.hearth/models` | Model download cache path |

## Bind from configuration

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

## Model selection guidance

- Prefer **Q4_K_M** for the best size-to-quality starting point.
- Increase `ContextSize` only when your prompts genuinely need it.
- Start with `GpuLayers = 0` on CPU-only machines.
- On supported GPU backends, increase `GpuLayers` gradually until memory pressure appears.

## Runtime behavior

Hearth loads model weights once, then creates short-lived executors per request. That gives you:

- safe concurrent calls
- predictable DI lifetime behavior
- independent KV caches per in-flight request
