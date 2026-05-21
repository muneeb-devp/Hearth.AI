using Hearth;
using Hearth.Samples.Blazor.Components;

var modelPath = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("HEARTH_MODEL") ?? "./model.gguf";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHearth(options =>
{
    options.Model = modelPath;
    options.ContextSize = 4096;
    options.GpuLayers = 0;
    options.BatchSize = 512;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
