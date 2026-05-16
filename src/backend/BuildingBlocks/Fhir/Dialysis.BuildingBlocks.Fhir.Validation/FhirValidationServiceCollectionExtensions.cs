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
        /// When <see cref="FhirAuthoringOptions.PackagesPath"/> is set, a startup preloader
        /// auto-loads external <c>.tgz</c> packages from that folder so declared IG
        /// dependencies resolve without a manual upload after every restart.
        /// </summary>
        public IServiceCollection AddFhirArtifactAuthoring(
            Action<FhirAuthoringOptions>? configure = null)
        {
            var options = new FhirAuthoringOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);
            services.TryAddSingleton<AuthoredConformanceRegistry>();
            services.TryAddSingleton<IFhirConformanceRegistry>(
                sp => sp.GetRequiredService<AuthoredConformanceRegistry>());
            services.TryAddSingleton<FhirJsonSerializerProvider>();
            services.TryAddSingleton<IFhirProfileFactory, FhirProfileFactory>();
            services.TryAddSingleton<IFhirImplementationGuideFactory, FhirImplementationGuideFactory>();
            services.TryAddSingleton<IFhirArtifactVerifier, FhirArtifactVerifier>();
            services.TryAddSingleton<IFhirArtifactAuthoringService, FhirArtifactAuthoringService>();
            services.TryAddSingleton<IFhirPackageLoader, FhirPackageLoader>();

            if (!string.IsNullOrWhiteSpace(options.PackagesPath))
                services.AddHostedService<FhirPackagePreloader>();

            return services;
        }
    }
}
