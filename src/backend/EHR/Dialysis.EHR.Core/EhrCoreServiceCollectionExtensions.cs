using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.EHR.Core;

/// <summary>
/// Registers EHR core defaults (clock) for the module's composition root.
/// </summary>
public static class EhrCoreServiceCollectionExtensions
{
    public static IServiceCollection AddEhrCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IEhrClock, EhrClock>();
        return services;
    }
}
