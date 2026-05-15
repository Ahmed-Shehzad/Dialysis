using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.EHR.Core;

/// <summary>
/// Registers EHR core defaults (clock) for the module's composition root.
/// </summary>
public static class EhrCoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEhrCore()
        {
            services.TryAddSingleton<IEhrClock, EhrClock>();
            return services;
        }
    }
}
