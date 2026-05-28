using Hearth;
using Hearth.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHearth(options =>
{
    options.Model = builder.Configuration["HEARTH__MODEL"]
        ?? builder.Configuration["HEARTH_MODEL"]
        ?? throw new InvalidOperationException(
            "Set HEARTH__MODEL to a Hugging Face repo ID (e.g. 'Qwen/Qwen2.5-0.5B-Instruct-GGUF') or a local .gguf path.");

    if (int.TryParse(builder.Configuration["HEARTH__GPULAYERS"], out var gpuLayers))
    {
        options.GpuLayers = gpuLayers;
    }

    if (int.TryParse(builder.Configuration["HEARTH__CONTEXTSIZE"], out var contextSize))
    {
        options.ContextSize = contextSize;
    }

    if (int.TryParse(builder.Configuration["HEARTH__BATCHSIZE"], out var batchSize))
    {
        options.BatchSize = batchSize;
    }

    var hfToken = builder.Configuration["HEARTH__HUGGINGFACETOKEN"];
    if (!string.IsNullOrEmpty(hfToken))
    {
        options.HuggingFaceToken = hfToken;
    }
});

var app = builder.Build();

app.MapHearth();
app.Run();
