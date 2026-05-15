using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

public static class FhirDeIdentificationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddFhirDeIdentification()
        {
            services.TryAddSingleton<IFhirDeIdentifier, SafeHarborDeIdentifier>();
            return services;
        }
    }
}
