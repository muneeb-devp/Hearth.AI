using Microsoft.Extensions.DependencyInjection;

namespace Hearth;

/// <summary>
/// A builder for configuring Hearth services. Returned by <see cref="ServiceCollectionExtensions.AddHearth"/>
/// to allow chaining extension methods from add-on packages such as <c>Hearth.AI.Rag</c>.
/// </summary>
public interface IHearthBuilder
{
    /// <summary>The underlying service collection.</summary>
    IServiceCollection Services { get; }
}
