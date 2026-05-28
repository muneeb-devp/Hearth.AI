using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using System.ClientModel;

namespace Hearth.Aspire;

/// <summary>Extension methods for wiring up <see cref="IChatClient"/> from an Aspire connection string.</summary>
public static class HearthAspireExtensions
{
    /// <summary>
    /// Registers <see cref="IChatClient"/> using the connection string Aspire injected under
    /// <paramref name="connectionName"/>. Connects to the Hearth inference server's
    /// OpenAI-compatible <c>/v1</c> endpoint.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">
    /// The name of the connection string set by Aspire — matches the resource name passed to
    /// <c>AddHearth()</c> in the AppHost project.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    public static IHostApplicationBuilder AddHearth(
        this IHostApplicationBuilder builder,
        string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var connectionString = builder.Configuration[$"ConnectionStrings:{connectionName}"]
                ?? throw new InvalidOperationException(
                    $"No connection string '{connectionName}' found. " +
                    $"Ensure you called WithReference(hearth) in your AppHost and used the same name here.");

            var endpoint = new Uri(connectionString);
            var openAiClient = new OpenAIClient(
                new ApiKeyCredential("hearth"),
                new OpenAIClientOptions { Endpoint = endpoint });

            return openAiClient.GetChatClient("hearth").AsIChatClient();
        });

        return builder;
    }
}
