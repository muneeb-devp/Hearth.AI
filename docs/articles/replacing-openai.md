# Replacing OpenAI with Hearth in an existing app

This guide walks through swapping an OpenAI-backed app to local inference via Hearth without changing any business logic. It takes about five minutes.

## The core insight

Both the OpenAI .NET SDK and Hearth implement `IChatClient` from `Microsoft.Extensions.AI`. If your app already depends on that abstraction, the swap is one line of registration code.

If your app talks directly to the OpenAI SDK, you have two paths:

- **Path A** — keep using the OpenAI client but route it to Hearth's `/v1` endpoint  
- **Path B** — replace the registration with `AddHearth()`

## Path A: point your existing OpenAI client at Hearth

This works with any OpenAI-compatible SDK — Python, TypeScript, Java, or the official .NET client. No code changes beyond the base URL.

Install the ASP.NET Core package and expose the `/v1` routes:

```bash
dotnet add package Hearth.AI
dotnet add package Hearth.AI.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddHearth(options =>
    options.Model = "./models/qwen2.5-7b-instruct-q4_k_m.gguf");

app.MapHearth();
```

Then in whatever client you already have, change only the base URL:

```csharp
// Before (OpenAI)
var client = new OpenAIClient("sk-...");

// After (Hearth)
var client = new OpenAIClient(
    new ApiKeyCredential("ignored"),
    new OpenAIClientOptions { Endpoint = new Uri("http://localhost:5000") });
```

Your existing code — `client.GetChatClient(...)`, streaming, tool calls — continues to work unchanged.

## Path B: replace the registration

If your services are already registered against `IChatClient`, the swap is a one-line change in `Program.cs`.

### Before (typical OpenAI + MEA setup)

```csharp
builder.Services.AddOpenAIClient()
    .AddChatClient("gpt-4o");
```

### After (Hearth)

```csharp
builder.Services.AddHearth(options =>
    options.Model = "./models/qwen2.5-7b-instruct-q4_k_m.gguf");
```

Every service that injects `IChatClient` now talks to the local model. Nothing else changes.

### Example service (unchanged either way)

```csharp
public sealed class SummaryService(IChatClient chat)
{
    public async Task<string> SummarizeAsync(string text, CancellationToken ct = default)
    {
        var response = await chat.GetResponseAsync(
        [
            new(ChatRole.System, "Summarize in two sentences."),
            new(ChatRole.User,   text)
        ], cancellationToken: ct);

        return response.Message.Text ?? string.Empty;
    }
}
```

## Handling temperature and max tokens

The `HearthOptions` class controls model-level defaults. Per-call overrides use the standard `ChatOptions`:

```csharp
var options = new ChatOptions
{
    Temperature = 0.3f,
    MaxOutputTokens = 500,
};

var response = await chat.GetResponseAsync(messages, options, ct);
```

## What to expect

| Aspect | OpenAI | Hearth (local) |
| --- | --- | --- |
| Latency (first token) | ~200–800 ms (network) | 50–300 ms (CPU/GPU dependent) |
| Throughput | Rate-limited by API tier | Limited by your hardware |
| Context window | 128k (gpt-4o) | 4–128k depending on model |
| Cost | Per-token billing | Hardware (fixed cost) |
| Data privacy | Sent to OpenAI servers | Never leaves your machine |

## Switching back

Because everything runs through `IChatClient`, switching back is the same one-line change in reverse. You can also toggle between backends by environment:

```csharp
if (builder.Environment.IsProduction() && builder.Configuration["UseCloud"] == "true")
    builder.Services.AddOpenAIClient().AddChatClient("gpt-4o");
else
    builder.Services.AddHearth(o => o.Model = builder.Configuration["Hearth:Model"]!);
```
