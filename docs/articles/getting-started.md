# Getting started

## Install the base package

```bash
dotnet add package Hearth.AI
```

## Download a GGUF model

Hearth works with ChatML-compatible GGUF models. A practical starting point is Qwen 2.5:

```bash
wget https://huggingface.co/Qwen/Qwen2.5-7B-Instruct-GGUF/resolve/main/qwen2.5-7b-instruct-q4_k_m.gguf
```

## Register Hearth

```csharp
builder.Services.AddHearth(options =>
{
    options.Model = "./models/qwen2.5-7b-q4_k_m.gguf";
    options.ContextSize = 8192;
    options.GpuLayers = 35;
});
```

`AddHearth(...)` registers:

- `IChatClient`
- `IEmbeddingGenerator<string, Embedding<float>>`
- the shared model state used by both

## Ask the model a question

```csharp
public sealed class SummaryService(IChatClient chat)
{
    public async Task<string> SummarizeAsync(string document, CancellationToken cancellationToken = default)
    {
        var response = await chat.GetResponseAsync(
        [
            new(ChatRole.System, "Summarize the following document in three sentences."),
            new(ChatRole.User, document)
        ], cancellationToken: cancellationToken);

        return response.Message.Text ?? string.Empty;
    }
}
```

## Stream responses

```csharp
await foreach (var update in chat.GetStreamingResponseAsync(
[
    new(ChatRole.User, "Write a haiku about autumn.")
]))
{
    Console.Write(update.Text);
}
```

## Next steps

- Review [Configuration](configuration.md) to tune context size, threading, and model download behavior.
- Review [GPU backends](gpu-backends.md) if you want hardware acceleration.
- Review [ASP.NET Core integration](aspnetcore.md) if you want OpenAI-compatible `/v1` endpoints.
- Review [Aspire integration](aspire.md) to orchestrate the inference server with .NET Aspire.
- Review [RAG pipeline](rag.md) to add document retrieval and grounded answers.
- Review [Blazor chat component](blazor-chat.md) to embed a streaming chat UI in a Blazor app.
