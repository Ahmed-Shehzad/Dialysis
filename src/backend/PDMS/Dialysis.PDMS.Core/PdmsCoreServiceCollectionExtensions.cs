using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.PDMS.Core;

public static class PdmsCoreServiceCollectionExtensions
{
    public static IServiceCollection AddPdmsCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IPdmsClock, PdmsClock>();
        return services;
    }
}
