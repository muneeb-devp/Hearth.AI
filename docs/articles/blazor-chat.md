# Blazor Chat Component

`Hearth.AI.Blazor` provides a drop-in streaming chat UI for Blazor Server and Blazor WebAssembly applications powered by your local LLM.

## Installation

```xml
<PackageReference Include="Hearth.AI" Version="0.2.0" />
<PackageReference Include="Hearth.AI.Blazor" Version="0.2.0" />
```

## Quick Start

Register both Hearth and the Blazor components in `Program.cs`:

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
});

builder.Services.AddHearthBlazor();
```

Drop the component into any `.razor` page:

```razor
@using Hearth.Blazor.Components

<HearthChat SystemPrompt="You are a helpful assistant." />
```

That's it — the component handles user input, streaming token output, conversation history, and scroll-to-bottom automatically.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SystemPrompt` | `string?` | `null` | System prompt prepended to every request |
| `Placeholder` | `string` | `"Ask me anything…"` | Input textarea placeholder |
| `EnableMarkdown` | `bool` | `true` | Render assistant responses as Markdown (via Markdig) |
| `Theme` | `HearthChatTheme` | `Default` | Visual theme |
| `Tools` | `IList<AITool>?` | `null` | MEA tools passed to `ChatOptions.Tools` |
| `ChatOptions` | `ChatOptions?` | `null` | Additional MEA chat options |
| `EmptyStateContent` | `RenderFragment?` | `null` | Custom empty state content |
| `MessageTemplate` | `RenderFragment<ChatEntry>?` | `null` | Custom message renderer |
| `OnMessageSent` | `EventCallback<string>` | — | Fires when the user sends a message |
| `OnResponseReceived` | `EventCallback<ChatEntry>` | — | Fires when the assistant completes a response |
| `OnError` | `EventCallback<Exception>` | — | Fires on streaming errors |

## Themes

Four built-in themes are available via the `HearthChatTheme` enum:

```razor
<HearthChat Theme="HearthChatTheme.Dark" SystemPrompt="..." />
```

| Theme | Description |
|-------|-------------|
| `Default` | Light background, blue user bubbles |
| `Dark` | Dark slate background |
| `Light` | White background with bordered assistant bubbles |
| `Minimal` | No bubble backgrounds, full-width, underline separator |

## Custom Empty State

```razor
<HearthChat SystemPrompt="You are helpful.">
    <EmptyStateContent>
        <div class="my-empty">
            <h3>Hello! How can I help you today?</h3>
        </div>
    </EmptyStateContent>
</HearthChat>
```

## Custom Message Template

Override the default message rendering entirely:

```razor
<HearthChat SystemPrompt="...">
    <MessageTemplate Context="entry">
        <div class="my-msg @(entry.Role == ChatRole.User ? "from-user" : "from-ai")">
            @entry.Content
            @if (entry.IsStreaming) { <span class="blink">▍</span> }
        </div>
    </MessageTemplate>
</HearthChat>
```

## Programmatic Control

Obtain a reference to the component and call its methods:

```razor
<HearthChat @ref="_chat" SystemPrompt="..." />

@code {
    private HearthChat? _chat;

    private void StopGeneration() => _chat?.Cancel();
    private void NewConversation()  => _chat?.ClearHistory();
}
```

| Method | Description |
|--------|-------------|
| `Cancel()` | Cancels the current streaming response |
| `ClearHistory()` | Clears conversation history and re-renders empty state |

## Tool Use

Pass MEA `AITool` instances to enable tool calling:

```csharp
var weatherTool = AIFunctionFactory.Create(
    ([Description("City name")] string city) => $"Sunny in {city}",
    "get_weather");
```

```razor
<HearthChat SystemPrompt="You can check the weather."
            Tools="@([weatherTool])" />
```

## Aspire + Blazor

When using `Hearth.AI.Aspire` to connect to a remote Hearth inference server, `IChatClient` is already registered — just add `AddHearthBlazor()` and drop the component in:

```csharp
// Program.cs
builder.AddHearth("ai");         // reads Aspire connection string
builder.Services.AddHearthBlazor();
```

```razor
<HearthChat SystemPrompt="Remote model powered by Aspire." />
```

## Implementing `IAsyncDisposable`

`HearthChat` implements `IAsyncDisposable` and cleans up its JS module reference and cancellation token source automatically when the component is removed from the render tree.
