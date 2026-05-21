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

Open `http://localhost:5000` after startup. The sample streams tokens into the UI as they are generated.

## What to study in the samples

- how `AddHearth(...)` is registered
- how `IChatClient` is injected
- how streaming responses are consumed
- how the app stays independent from the underlying inference host
