using Aspire.Hosting.ApplicationModel;

namespace Hearth.Aspire.Hosting;

/// <summary>
/// Represents a Hearth inference server container resource in the Aspire application model.
/// Exposes an OpenAI-compatible <c>/v1</c> endpoint that consuming projects connect to via
/// <c>IChatClient</c>.
/// </summary>
public sealed class HearthResource(string name)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const int DefaultPort = 5000;

    private EndpointReference? _endpoint;

    internal EndpointReference Endpoint => _endpoint ??= new EndpointReference(this, "http");

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"http://{Endpoint.Property(EndpointProperty.Host)}:{Endpoint.Property(EndpointProperty.Port)}/v1");
}
