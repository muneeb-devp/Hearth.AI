using Microsoft.Extensions.DependencyInjection;

namespace Hearth;

internal sealed class HearthBuilder(IServiceCollection services) : IHearthBuilder
{
    public IServiceCollection Services => services;
}
