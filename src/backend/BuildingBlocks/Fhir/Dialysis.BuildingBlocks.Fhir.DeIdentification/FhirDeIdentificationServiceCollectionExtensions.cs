using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

public static class FhirDeIdentificationServiceCollectionExtensions
{
    public static IServiceCollection AddFhirDeIdentification(this IServiceCollection services)
    {
        services.TryAddSingleton<IFhirDeIdentifier, SafeHarborDeIdentifier>();
        return services;
    }
}
