using System.Text;
using Hearth;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var modelPath = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("HEARTH_MODEL") ?? "./model.gguf";

var builder = Host.CreateApplicationBuilder(args);

builder.Logging
    .ClearProviders()
    .AddConsole()
    .SetMinimumLevel(LogLevel.Warning);

builder.Services.AddHearth(options =>
{
    options.Model = modelPath;
    options.ContextSize = 4096;
    options.GpuLayers = 0;
    options.BatchSize = 512;
});

var host = builder.Build();

Console.WriteLine("╔══════════════════════════════╗");
Console.WriteLine("║        Hearth Chat           ║");
Console.WriteLine("╚══════════════════════════════╝");
Console.WriteLine($"Model : {modelPath}");
Console.WriteLine("Quit  : type 'exit' or press Ctrl+C");
Console.WriteLine();

var chat = host.Services.GetRequiredService<IChatClient>();
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var history = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful, concise assistant.")
};

while (!cts.IsCancellationRequested)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (input is null || cts.IsCancellationRequested)
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    history.Add(new ChatMessage(ChatRole.User, input));

    Console.Write("\nAssistant: ");
    var response = new StringBuilder();

    try
    {
        await foreach (var update in chat.GetStreamingResponseAsync(history, cancellationToken: cts.Token))
        {
            var text = update.Text ?? string.Empty;
            Console.Write(text);
            response.Append(text);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n[interrupted]");
        break;
    }

    Console.WriteLine("\n");
    history.Add(new ChatMessage(ChatRole.Assistant, response.ToString().Trim()));
}
