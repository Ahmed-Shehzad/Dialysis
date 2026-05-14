using Hl7.Fhir.Model;

namespace Dialysis.HIE.Core.Coding;

/// <summary>
/// Resolves a named clinical concept (see <see cref="ClinicalConcepts"/>) to a runtime
/// <see cref="CodeableConcept"/>. Mappers depend on this — never on raw code-string constants —
/// so the underlying terminology server is the single source of truth for which code represents
/// which concept.
/// </summary>
public interface IConceptCatalog
{
    /// <summary>Returns the registered concept, throwing if the name is unknown.</summary>
    CodeableConcept Get(string conceptName);

    /// <summary>Returns the registered concept or <c>null</c> when the name is unknown.</summary>
    CodeableConcept? TryGet(string conceptName);
}

/// <summary>One catalog entry: system URI, code value, fallback display.</summary>
public sealed record ConceptCatalogEntry(string Name, string System, string Code, string FallbackDisplay);
