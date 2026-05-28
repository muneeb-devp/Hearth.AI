# Aspire integration

Hearth ships two packages for .NET Aspire:

- `Hearth.AI.Aspire.Hosting` — used in the **AppHost** project to declare the Hearth inference server as an Aspire resource.
- `Hearth.AI.Aspire` — used in **consuming service projects** to receive `IChatClient` from the Aspire-injected connection string.

## Install the packages

In the AppHost project:

```bash
dotnet add package Hearth.AI.Aspire.Hosting
```

In each service that needs to talk to the inference server:

```bash
dotnet add package Hearth.AI.Aspire
```

## AppHost setup

Call `AddHearth` on the distributed application builder, then chain configuration methods to describe the model and how the container should run.

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var hearth = builder.AddHearth("ai")
    .WithModel("Qwen/Qwen2.5-7B-Instruct-GGUF")
    .WithContextSize(8192);

builder.AddProject<Projects.MyApi>("api")
    .WithReference(hearth);

builder.Build().Run();
```

`WithReference(hearth)` injects a `ConnectionStrings__ai` environment variable into `MyApi` at runtime. The value is the base URL of the Hearth container's `/v1` endpoint.

## Service project setup

In any project that received a `WithReference(hearth)` reference, call `AddHearth` with the same name you gave the resource:

```csharp
// MyApi/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddHearth("ai");   // reads ConnectionStrings__ai injected by Aspire

var app = builder.Build();
app.Run();
```

`AddHearth` registers a singleton `IChatClient` backed by an OpenAI-compatible HTTP client pointed at the Hearth container. Inject `IChatClient` anywhere in your service as normal.

## How the connection string flows

1. The AppHost resolves the container's host and port at startup and writes `http://<host>:<port>/v1` under `ConnectionStrings__ai`.
2. The Aspire service defaults service discovery and environment variable injection handle propagation — no manual configuration is needed.
3. `AddHearth("ai")` in the service project reads `ConnectionStrings:ai` from `IConfiguration` and constructs an `OpenAIClient` pointed at that endpoint, then adapts it to `IChatClient` via `Microsoft.Extensions.AI`.

## Minimal full example

The simplest useful setup is two projects: an AppHost and one API service.

**AppHost/Program.cs**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var hearth = builder.AddHearth("ai")
    .WithModel("Qwen/Qwen2.5-7B-Instruct-GGUF");

builder.AddProject<Projects.ChatApi>("chatapi")
    .WithReference(hearth);

builder.Build().Run();
```

**ChatApi/Program.cs**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddHearth("ai");

var app = builder.Build();

app.MapPost("/chat", async (string message, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(
    [
        new(ChatRole.User, message)
    ]);
    return response.Message.Text;
});

app.Run();
```

Run `dotnet run --project AppHost` and the Aspire dashboard will show both resources. The inference server starts downloading the model on first boot and exposes `POST /v1/chat/completions` on port 5000.

## Caching models with WithModelCacheMount

By default, the container downloads the model from Hugging Face each time it starts. Bind-mount a host directory to `/app/models` to keep downloaded models across restarts:

```csharp
var hearth = builder.AddHearth("ai")
    .WithModel("Qwen/Qwen2.5-7B-Instruct-GGUF")
    .WithModelCacheMount("/data/hearth-models");
```

The host path is created automatically if it does not exist. On developer machines a path under the home directory works well; in CI or production, mount a volume backed by fast storage.

## GPU acceleration with WithGpuAcceleration

Pass the number of model layers to offload to the GPU. Pass `999` (the default) to offload all layers, or a specific integer to keep some layers on CPU — useful when VRAM is limited.

```csharp
var hearth = builder.AddHearth("ai")
    .WithModel("Qwen/Qwen2.5-7B-Instruct-GGUF")
    .WithGpuAcceleration()         // offload all layers
    .WithModelCacheMount("/data/hearth-models");
```

Partial offload example:

```csharp
.WithGpuAcceleration(layers: 20)  // first 20 layers on GPU, rest on CPU
```

GPU acceleration requires a host with a compatible GPU and the appropriate container runtime configuration (NVIDIA Container Toolkit or equivalent). See [GPU backends](gpu-backends.md) for details.

## Gated models with WithHuggingFaceToken

Some Hugging Face repositories require an access token. Declare the token as an Aspire parameter and pass it to the resource builder:

```csharp
var hfToken = builder.AddParameter("hf-token", secret: true);

var hearth = builder.AddHearth("ai")
    .WithModel("meta-llama/Meta-Llama-3-8B-Instruct-GGUF")
    .WithHuggingFaceToken(hfToken);
```

Aspire parameters can be sourced from `appsettings.json`, user secrets, or environment variables. For local development, store the token in user secrets:

```bash
dotnet user-secrets set "Parameters:hf-token" "hf_..."  --project AppHost
```

The token is passed to the container as `HEARTH__HUGGINGFACETOKEN` and is never written to any output file or build artifact.

## Combining with Hearth.AI.Blazor

`Hearth.AI.Aspire` registers `IChatClient`, which is exactly what `Hearth.AI.Blazor` expects. Add the Blazor package, call `AddHearthBlazor()`, and drop the component into any Razor page:

**ChatApp/Program.cs**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddHearth("ai");           // IChatClient from Aspire connection string
builder.Services.AddHearthBlazor();

var app = builder.Build();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

**Pages/Chat.razor**

```razor
@using Hearth.Blazor.Components

<HearthChat SystemPrompt="You are a helpful assistant." />
```

No further wiring is needed. The component resolves `IChatClient` from the DI container and streams responses directly to the browser. See [Blazor Chat Component](blazor-chat.md) for the full component reference.

## OpenTelemetry

`AddHearth` in the AppHost calls `.WithOtlpExporter()` automatically. The inference server's traces, metrics, and logs are forwarded to the Aspire dashboard's built-in OTLP collector without any extra configuration. Open the dashboard after `dotnet run --project AppHost` to see inference latency and token throughput alongside your other services.
