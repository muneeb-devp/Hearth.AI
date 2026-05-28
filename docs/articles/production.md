# Deploying Hearth to production

Hearth is a library that runs inside your ASP.NET Core process. There is no separate inference server to manage. This guide covers containerization, hardware sizing, performance tuning, and operational considerations.

## Docker

The repository ships with a `Dockerfile`. Build and run it with your model:

```bash
# Build the image
docker build -t hearth-ai/demo .

# Run with a local model directory bind-mounted
docker run -p 5000:5000 \
  -v /data/models:/app/models \
  -e HEARTH_MODEL=/app/models/qwen2.5-7b-instruct-q4_k_m.gguf \
  hearth-ai/demo
```

Then hit it with any OpenAI-compatible client:

```bash
curl http://localhost:5000/v1/models
```

### GPU pass-through (CUDA)

```bash
docker run --gpus all -p 5000:5000 \
  -v /data/models:/app/models \
  -e HEARTH_MODEL=/app/models/qwen2.5-7b-instruct-q4_k_m.gguf \
  hearth-ai/demo
```

The base image uses the CUDA runtime variant when `Hearth.AI.Cuda` is installed. Requires the NVIDIA Container Toolkit on the host.

## Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hearth
spec:
  replicas: 1
  selector:
    matchLabels:
      app: hearth
  template:
    metadata:
      labels:
        app: hearth
    spec:
      containers:
      - name: hearth
        image: hearth-ai/demo:latest
        ports:
        - containerPort: 5000
        env:
        - name: HEARTH_MODEL
          value: /models/qwen2.5-7b-instruct-q4_k_m.gguf
        - name: Hearth__ContextSize
          value: "4096"
        - name: Hearth__GpuLayers
          value: "35"
        volumeMounts:
        - name: models
          mountPath: /models
        resources:
          requests:
            memory: "12Gi"
          limits:
            memory: "16Gi"
      volumes:
      - name: models
        persistentVolumeClaim:
          claimName: model-pvc
```

> **Important**: set `replicas: 1` unless your model fits multiple times in the node's memory. Each pod loads a full copy of the weights.

## Configuration via environment variables

All `HearthOptions` properties can be set via environment variables using the `Hearth__` prefix:

```bash
Hearth__Model=/models/qwen2.5-7b-instruct-q4_k_m.gguf
Hearth__ContextSize=8192
Hearth__GpuLayers=35
Hearth__BatchSize=512
Hearth__Threads=8
```

## Performance tuning

### CPU inference

```csharp
options.Threads = Environment.ProcessorCount;  // use all cores
options.BatchSize = 1024;                       // larger batches for throughput
```

`Threads = -1` (the default) lets llama.cpp pick the count automatically. For dedicated inference boxes, setting it explicitly to physical core count (not logical) often improves throughput.

### GPU inference

```csharp
options.GpuLayers = 999;   // offload as much as VRAM allows
options.BatchSize = 512;   // start here; increase if VRAM allows
```

Monitor VRAM usage with `nvidia-smi` (CUDA) or `sudo powermetrics --samplers gpu_power` (Apple Silicon) to find the sweet spot.

### Context size vs throughput

Each concurrent request allocates its own KV cache. With 5 concurrent requests at `ContextSize = 4096`, you need ~2.5 GB for KV caches alone (for a 7B model). Calculate accordingly:

```
KV cache ≈ 2 × ContextSize × Layers × Heads × HeadDim × sizeof(float16)
```

For a 7B model at 4096 tokens: roughly 512 MB per context.

## Concurrency

Hearth creates one `StatelessExecutor` per inference call. Concurrent requests run in separate contexts derived from the same shared `LLamaWeights`. The weights are read-only; there are no thread-safety concerns at the weights level.

CPU inference throughput scales linearly with cores up to roughly 8–12 threads per request. If you have many cores, consider running multiple processes behind a load balancer rather than one process with many threads.

## Memory considerations

| Component | Approximate size |
| --- | --- |
| Model weights (7B Q4_K_M) | 4.5 GB |
| KV cache per active context (4096 tokens) | 512 MB |
| .NET runtime + overhead | ~200 MB |

Set your container's memory limit to at minimum `model_size + (max_concurrent × kv_cache_per_context) + 512 MB`.

## Health check

Add a simple health endpoint alongside `MapHearth()`:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
```

Or use the built-in `GET /v1/models` endpoint — if it returns 200, the model is loaded and inference is available.

## Logging

Hearth emits structured log events via `Microsoft.Extensions.Logging`. In production, set the minimum level appropriately:

```json
{
  "Logging": {
    "LogLevel": {
      "Hearth": "Warning"
    }
  }
}
```

The `Hearth` category emits `model-loaded` and `model-loading` events at `Information` level, and all inference diagnostics at `Debug` level. Filter to `Warning` in production to reduce noise.
