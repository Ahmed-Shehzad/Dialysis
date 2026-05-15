using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.PDMS.Core;

public static class PdmsCoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPdmsCore()
        {
            services.TryAddSingleton<IPdmsClock, PdmsClock>();
            return services;
        }
    }
}
