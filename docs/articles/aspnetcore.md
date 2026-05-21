# ASP.NET Core integration

Install the ASP.NET Core package when you want OpenAI-compatible endpoints on top of the same local model runtime.

```bash
dotnet add package Hearth.AI.AspNetCore
```

## Register the model and map the endpoints

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
});

var app = builder.Build();

app.MapHearth();

app.Run();
```

## Exposed routes

`MapHearth()` wires up:

- `POST /v1/chat/completions`
- `POST /v1/embeddings`
- `GET /v1/models`

These routes are designed for OpenAI-compatible clients while keeping execution local to your app host.

## When to use it

This package is a good fit when you want:

- a drop-in local endpoint for existing OpenAI SDK integrations
- a self-hosted inference gateway inside an internal network
- streaming chat responses over Server-Sent Events

## Package relationship

`Hearth.AI.AspNetCore` depends on the base `Hearth.AI` package. Register the model once with `AddHearth(...)`, then expose the endpoints with `MapHearth()`.
