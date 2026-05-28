using Hearth;
using Hearth.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Hearth: local LLM inference ──────────────────────────────────────────────
// Set HEARTH_MODEL env var or update appsettings.json to point to your GGUF file.
builder.Services.AddHearth(options =>
    builder.Configuration.GetSection("Hearth").Bind(options));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── OpenAI-compatible endpoints ───────────────────────────────────────────────
// POST /v1/chat/completions  (streaming + non-streaming)
// POST /v1/embeddings
// GET  /v1/models
app.MapHearth();

app.Run();
