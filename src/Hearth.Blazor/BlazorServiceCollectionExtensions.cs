using Markdig;
using Microsoft.Extensions.DependencyInjection;

namespace Hearth.Blazor;

public static class BlazorServiceCollectionExtensions
{
    /// <summary>
    /// Adds Hearth Blazor components. Registers a Markdig pipeline singleton
    /// for markdown rendering. Requires IChatClient to already be registered
    /// (via AddHearth or AddHearth+Aspire).
    /// </summary>
    public static IServiceCollection AddHearthBlazor(this IServiceCollection services)
    {
        services.AddSingleton(_ =>
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
        return services;
    }
}
