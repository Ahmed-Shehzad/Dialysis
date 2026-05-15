using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.HIE.Core.Coding;

public static class ConceptCatalogServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="ConceptCatalog"/> as an <see cref="IConceptCatalog"/> with the HIE's
        /// canonical concept seed (renal dialysis, successful outcome, clinical-note type). Additional
        /// concepts can be supplied via <paramref name="configure"/>; module-specific concepts should be
        /// registered from the owning module's composition.
        ///
        /// Also wires the <see cref="ConceptCatalogValidatorHostedService"/> that validates every entry
        /// against the upstream terminology server at startup (no-op when terminology is unavailable).
        /// </summary>
        public IServiceCollection AddHieConceptCatalog(
            Action<List<ConceptCatalogEntry>>? configure = null)
        {
            var seed = new List<ConceptCatalogEntry>
        {
            new(ClinicalConcepts.RenalDialysis,         CodeSystems.SnomedCt, "265764009", "Renal dialysis"),
            new(ClinicalConcepts.SuccessfulOutcome,     CodeSystems.SnomedCt, "385669000", "Successful"),
            new(ClinicalConcepts.SubsequentEvaluationNote, CodeSystems.Loinc, "11506-3",   "Subsequent evaluation note"),
        };
            configure?.Invoke(seed);

            var catalog = new ConceptCatalog(seed);
            services.TryAddSingleton(catalog);
            services.TryAddSingleton<IConceptCatalog>(sp => sp.GetRequiredService<ConceptCatalog>());
            services.AddHostedService<ConceptCatalogValidatorHostedService>();
            return services;
        }
    }
}
