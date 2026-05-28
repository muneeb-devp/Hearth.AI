# Choosing the right model for your hardware

Running a model locally means the model must fit (mostly) in RAM or VRAM. This guide helps you pick a quantization and size that suits your machine.

## Quick reference: model size vs. RAM

| Model family | Parameters | Q4_K_M size | Minimum RAM | Recommended RAM |
| --- | --- | --- | --- | --- |
| Phi-3.5 Mini | 3.8B | ~2.3 GB | 4 GB | 8 GB |
| Qwen 2.5 | 7B | ~4.5 GB | 8 GB | 16 GB |
| Llama 3.1 | 8B | ~5.0 GB | 8 GB | 16 GB |
| Mistral NeMo | 12B | ~7.5 GB | 12 GB | 24 GB |
| Llama 3.3 | 70B | ~43 GB | 64 GB | 80 GB |

> Rule of thumb: model file size + ~2 GB for the KV cache and runtime overhead.

## Quantization formats explained

GGUF files ship in several quantization levels. The suffix tells you the trade-off.

| Suffix | Bits/weight | Quality | File size | When to use |
| --- | --- | --- | --- | --- |
| `Q2_K` | ~2.6 | Low | Smallest | Memory-constrained edge devices |
| `Q4_K_S` | ~4.4 | Good | Small | ~Same quality as Q4_K_M, slightly smaller |
| `Q4_K_M` | ~4.8 | Very good | Medium | **Best default for most workloads** |
| `Q5_K_M` | ~5.7 | Excellent | Larger | When you have headroom and want near-F16 |
| `Q8_0` | 8.0 | Near lossless | Large | Benchmarking / highest-quality local |
| `F16` | 16.0 | Lossless | Very large | Not recommended for inference; use for fine-tuning |

**Start with `Q4_K_M`.** It gives 95%+ of F16 quality at roughly 30% of the size.

## GPU offloading (`GpuLayers`)

The `GpuLayers` option controls how many transformer layers are offloaded to the GPU. More layers = faster inference but more VRAM.

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-instruct-q4_k_m.gguf";
    options.GpuLayers = 35;   // tune this for your VRAM budget
});
```

### VRAM budget guide for a 7B Q4_K_M model (~4.5 GB)

| Available VRAM | Suggested `GpuLayers` | Effect |
| --- | --- | --- |
| 4 GB | 10–15 | Partial offload; mixed CPU/GPU |
| 6 GB | 20–28 | Most layers on GPU |
| 8 GB | 32–35 | Full offload; fastest |
| 12 GB+ | 999 | Offload everything including KV cache |

Use `999` to offload as many layers as the backend allows. LLamaSharp will silently cap it at the model's actual layer count.

### Apple Silicon (Metal)

On a MacBook with unified memory, GPU and CPU share the same pool:

```csharp
options.GpuLayers = 999; // Metal, unified memory — offload everything
```

Install `Hearth.AI.Metal` and set `GpuLayers = 999`. Most 7–13B models run well even on an 8 GB M-series chip because there is no VRAM/RAM split.

## Context size (`ContextSize`)

Larger context windows cost memory quadratically (KV cache). Only raise it when you need it.

| Use case | Recommended `ContextSize` |
| --- | --- |
| Short Q&A, chat | 2048–4096 |
| Document summarization | 8192–16384 |
| Long document RAG | 32768+ |

A 7B model with `ContextSize = 4096` needs roughly 512 MB KV cache. With `ContextSize = 32768`, that grows to ~4 GB.

## Recommended starting configurations

### Developer laptop (16 GB RAM, no discrete GPU)

```csharp
options.Model = "./models/qwen2.5-7b-instruct-q4_k_m.gguf";
options.ContextSize = 4096;
options.GpuLayers = 0;
options.Threads = 8;
```

### Gaming PC (16 GB RAM, 8 GB VRAM — CUDA)

```csharp
options.Model = "./models/qwen2.5-7b-instruct-q4_k_m.gguf";
options.ContextSize = 8192;
options.GpuLayers = 35;
```

### Apple Silicon MacBook Pro (24 GB unified memory)

```csharp
options.Model = "./models/llama-3.1-8b-instruct-q5_k_m.gguf";
options.ContextSize = 16384;
options.GpuLayers = 999;
```

### Server (64 GB RAM, A100 80 GB VRAM)

```csharp
options.Model = "./models/llama-3.3-70b-instruct-q4_k_m.gguf";
options.ContextSize = 32768;
options.GpuLayers = 999;
```

## Where to download models

Hearth works with any GGUF model. The most practical source is [Hugging Face](https://huggingface.co/). Look for repos maintained by `bartowski` or `unsloth` — they publish high-quality GGUF conversions with consistent naming conventions.

You can also let Hearth download the model automatically by passing a Hugging Face repo ID:

```csharp
options.Model = "bartowski/Qwen2.5-7B-Instruct-GGUF";
options.ModelFile = "Qwen2.5-7B-Instruct-Q4_K_M.gguf";
```

The file is cached in `~/.hearth/models` and reused on subsequent runs.
