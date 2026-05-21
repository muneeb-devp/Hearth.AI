# GPU backends

The base `Hearth.AI` package gives you CPU inference. Install one of the backend packages when you want hardware acceleration.

| Package | Hardware target | Notes |
| --- | --- | --- |
| `Hearth.AI` | CPU | Default package |
| `Hearth.AI.Cuda` | NVIDIA CUDA 12 | Best fit for NVIDIA GPUs |
| `Hearth.AI.Metal` | Apple Silicon and supported macOS GPUs | Best fit for macOS |
| `Hearth.AI.Vulkan` | Vulkan-capable AMD and Intel GPUs | Cross-vendor GPU path |

## Choosing `GpuLayers`

`GpuLayers` controls how much of the model is offloaded:

- `0` = CPU only
- `35` = a practical starting point for many 7B Q4 models on 8 GB GPUs
- `999` = offload everything possible

## Practical guidance

- Start conservative and increase `GpuLayers` while watching VRAM usage.
- Use `999` on Apple Silicon when Metal is available and the model fits.
- Keep `ContextSize` realistic, because a larger KV cache increases memory use quickly.

## Installation examples

```bash
dotnet add package Hearth.AI.Cuda
```

```bash
dotnet add package Hearth.AI.Metal
```

```bash
dotnet add package Hearth.AI.Vulkan
```
