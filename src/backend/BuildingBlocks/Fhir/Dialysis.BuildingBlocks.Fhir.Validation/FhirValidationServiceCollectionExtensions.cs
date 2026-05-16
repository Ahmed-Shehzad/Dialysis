using Dialysis.BuildingBlocks.Fhir.Serialization;
using Dialysis.BuildingBlocks.Fhir.Validation.Authoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.Validation;

public static class FhirValidationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="IFhirProfileValidator"/> with the supplied profile bindings.
        /// US Core / USCDI URLs are bound by the caller via <see cref="FhirProfileMap.Require{T}(string)"/>.
        /// </summary>
        public IServiceCollection AddFhirProfileValidation(
            Action<FhirProfileMap> configure)
        {
            var map = new FhirProfileMap();
            configure(map);
            services.TryAddSingleton(map);
            services.TryAddSingleton<IFhirProfileValidator, DefaultFhirProfileValidator>();
            return services;
        }

        /// <summary>
        /// Registers the on-demand FHIR profile / Implementation Guide authoring pipeline:
        /// builder factories, the snapshot-backed correctness verifier, and the in-memory
        /// conformance registry that makes authored canonicals resolvable on the fly. The
        /// registry doubles as the base-spec resolver (bundled FHIR R4 core specification).
        /// </summary>
        public IServiceCollection AddFhirArtifactAuthoring()
        {
            services.TryAddSingleton<AuthoredConformanceRegistry>();
            services.TryAddSingleton<IFhirConformanceRegistry>(
                sp => sp.GetRequiredService<AuthoredConformanceRegistry>());
            services.TryAddSingleton<FhirJsonSerializerProvider>();
            services.TryAddSingleton<IFhirProfileFactory, FhirProfileFactory>();
            services.TryAddSingleton<IFhirImplementationGuideFactory, FhirImplementationGuideFactory>();
            services.TryAddSingleton<IFhirArtifactVerifier, FhirArtifactVerifier>();
            services.TryAddSingleton<IFhirArtifactAuthoringService, FhirArtifactAuthoringService>();
            return services;
        }
    }
}
