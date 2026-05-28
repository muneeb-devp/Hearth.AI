# Tool calling with Hearth

Hearth supports the `Microsoft.Extensions.AI` tool-calling contract. You define tools as .NET methods, pass them via `ChatOptions`, and the model will call them automatically.

## How it works

1. You annotate a static method with `[Description]` attributes.
2. You create an `AIFunction` from it.
3. You pass the function in `ChatOptions.Tools`.
4. Hearth's agentic loop invokes the function when the model emits a tool call, feeds the result back, and continues until the model produces a final text response (up to 5 rounds).

## Minimal example

```csharp
using Microsoft.Extensions.AI;
using System.ComponentModel;

// ── Define tools ─────────────────────────────────────────────────────────────

[Description("Returns the current UTC time.")]
static string GetCurrentTime() => DateTime.UtcNow.ToString("O");

[Description("Looks up the current weather for a city. Returns a brief summary.")]
static string GetWeather([Description("City name")] string city)
    => $"It is currently 22°C and sunny in {city}."; // replace with real API call

var tools = new List<AITool>
{
    AIFunctionFactory.Create(GetCurrentTime),
    AIFunctionFactory.Create(GetWeather),
};

// ── Call the model with tools ─────────────────────────────────────────────────

var chat = host.Services.GetRequiredService<IChatClient>();

var response = await chat.GetResponseAsync(
[
    new(ChatRole.System, "You are a helpful assistant with access to real-time tools."),
    new(ChatRole.User,   "What time is it, and what's the weather in Tokyo?"),
],
new ChatOptions { Tools = tools });

Console.WriteLine(response.Message.Text);
```

## Streaming with tools

Tool calling and streaming work together. Hearth collects tool-call deltas, invokes the function, and resumes streaming:

```csharp
await foreach (var update in chat.GetStreamingResponseAsync(messages, new ChatOptions { Tools = tools }))
{
    if (update.Text is not null)
        Console.Write(update.Text);
}
```

## Disabling automatic tool invocation

If you want to inspect tool calls before invoking them, set `ToolMode` to `None` and handle the loop yourself:

```csharp
var options = new ChatOptions
{
    Tools = tools,
    ToolMode = ChatToolMode.None,
};

var response = await chat.GetResponseAsync(messages, options);

foreach (var msg in response.Messages)
{
    foreach (var call in msg.Contents.OfType<FunctionCallContent>())
    {
        Console.WriteLine($"Model wants to call: {call.Name}({call.Arguments})");
    }
}
```

## Full worked example: code reviewer bot

```csharp
using Microsoft.Extensions.AI;
using System.ComponentModel;

// Tools
[Description("Reads a file from the repository.")]
static string ReadFile([Description("Relative file path")] string path)
    => File.Exists(path) ? File.ReadAllText(path) : $"File not found: {path}";

[Description("Lists files in a directory.")]
static string ListFiles([Description("Directory path")] string dir)
    => Directory.Exists(dir)
        ? string.Join("\n", Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        : $"Directory not found: {dir}";

var tools = new List<AITool>
{
    AIFunctionFactory.Create(ReadFile),
    AIFunctionFactory.Create(ListFiles),
};

// Conversation
var messages = new List<ChatMessage>
{
    new(ChatRole.System,
        "You are a code reviewer. Use ReadFile to inspect files and ListFiles to explore the repo. " +
        "Identify any obvious bugs and summarize your findings."),
    new(ChatRole.User, "Review the files in ./src and look for any issues."),
};

var response = await chat.GetResponseAsync(messages, new ChatOptions { Tools = tools });
Console.WriteLine(response.Message.Text);
```

## Notes on model compatibility

Tool calling requires a model that was fine-tuned for function calling. Most instruction-tuned models from the past year support it. If you see the model producing raw JSON instead of a final answer, try a more capable model or a higher quantization level.

Models known to work well with Hearth's tool-calling loop:

- Qwen 2.5 Instruct (7B and above)
- Llama 3.1/3.3 Instruct
- Mistral NeMo Instruct
- Phi-3.5 Mini Instruct (3.8B — fast but less reliable for complex tool chains)
