# Samples

The repository ships with runnable sample applications that exercise the main integration paths.

## Console sample

Project: `samples/Hearth.Samples.Console`

```bash
dotnet run --project samples/Hearth.Samples.Console -- ./models/qwen2.5-7b-q4_k_m.gguf
```

You can also provide the model path through `HEARTH_MODEL`.

The console sample is useful for:

- quick sanity checks against a local model
- streaming output experiments
- prompt iteration without building a full app

## Blazor sample

Project: `samples/Hearth.Samples.Blazor`

```bash
dotnet run --project samples/Hearth.Samples.Blazor -- ./models/qwen2.5-7b-q4_k_m.gguf
```

Open `http://localhost:5000` after startup. The sample streams tokens into the UI as they are generated using the `Hearth.AI.Blazor` component library.

## Inference server

Project: `samples/Hearth.Samples.Server`

A minimal ASP.NET Core app that exposes OpenAI-compatible `/v1` endpoints. This is the image that Aspire pulls when you use `Hearth.AI.Aspire.Hosting`. It can also be run standalone:

```bash
HEARTH__MODEL=Qwen/Qwen2.5-7B-Instruct-GGUF dotnet run --project samples/Hearth.Samples.Server
```

The server is also published as a container image:

```bash
docker run -p 5000:5000 \
  -e HEARTH__MODEL=Qwen/Qwen2.5-7B-Instruct-GGUF \
  ghcr.io/muneeb-devp/hearth-server:latest
```

All configuration is via environment variables:

| Variable | Default | Description |
|---|---|---|
| `HEARTH__MODEL` | _(required)_ | HuggingFace repo ID or local path |
| `HEARTH__GPULAYERS` | `0` | GPU layers to offload |
| `HEARTH__CONTEXTSIZE` | `4096` | Context window size |
| `HEARTH__BATCHSIZE` | `512` | Prompt-processing batch size |
| `HEARTH__HUGGINGFACETOKEN` | _(optional)_ | Token for gated model repos |

## What to study in the samples

- how `AddHearth(...)` is registered
- how `IChatClient` is injected
- how streaming responses are consumed
- how the app stays independent from the underlying inference host
