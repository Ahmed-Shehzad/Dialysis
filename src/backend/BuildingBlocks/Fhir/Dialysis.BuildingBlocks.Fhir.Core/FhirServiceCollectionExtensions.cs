using Dialysis.BuildingBlocks.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir;

public static class FhirServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the cross-cutting FHIR R4 building block. Idempotent — modules call this once
        /// inside their composition extension and configure resource readers/searchers/mappers via
        /// the <see cref="FhirBuilder"/> callback.
        /// </summary>
        public IServiceCollection AddFhir(Action<FhirBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services.TryAddSingleton<FhirResourceRegistry>();
            services.TryAddSingleton<FhirJsonSerializerProvider>();
            services.TryAddSingleton<IFhirCapabilityProvider, DefaultFhirCapabilityProvider>();
            services.TryAddScoped<IFhirConsentGate, NoOpConsentGate>();

            var registry = BuildRegistrySingleton(services);
            configure(new FhirBuilder(services, registry));
            return services;
        }
    }

    private static FhirResourceRegistry BuildRegistrySingleton(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(FhirResourceRegistry));
        if (existing?.ImplementationInstance is FhirResourceRegistry instance)
        {
            return instance;
        }

        var registry = new FhirResourceRegistry();
        services.RemoveAll<FhirResourceRegistry>();
        services.AddSingleton(registry);
        return registry;
    }
}
