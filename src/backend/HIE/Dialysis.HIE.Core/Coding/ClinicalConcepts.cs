namespace Dialysis.HIE.Core.Coding;

/// <summary>
/// Named clinical-concept identifiers HIE uses across mappers. Each name corresponds to one
/// FHIR Coding (system + code + canonical display) — the actual values are resolved at runtime
/// by <see cref="IConceptCatalog"/>, which consults the upstream terminology service to validate
/// existence and may refresh the display from the authoritative source.
///
/// To add a new concept: register it in <see cref="ConceptCatalogServiceCollectionExtensions"/>
/// or in a module's composition (preferred — modules own the concepts they emit).
/// </summary>
public static class ClinicalConcepts
{
    /// <summary>SNOMED CT "Renal dialysis" procedure (code 265764009).</summary>
    public const string RenalDialysis = "hie.procedure.renal-dialysis";

    /// <summary>SNOMED CT "Successful" outcome (code 385669000).</summary>
    public const string SuccessfulOutcome = "hie.outcome.successful";

    /// <summary>LOINC "Subsequent evaluation note" DocumentReference type (code 11506-3).</summary>
    public const string SubsequentEvaluationNote = "hie.document.subsequent-evaluation-note";
}
