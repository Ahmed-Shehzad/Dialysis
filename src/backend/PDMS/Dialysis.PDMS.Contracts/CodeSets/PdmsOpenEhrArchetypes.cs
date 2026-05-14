namespace Dialysis.PDMS.Contracts.CodeSets;

/// <summary>
/// openEHR archetype identifiers PDMS projects to. Names follow the openEHR CKM naming convention
/// so a future EHRbase swap pulls the matching .adl/.opt definitions from the registry by id.
/// </summary>
public static class PdmsOpenEhrArchetypes
{
    /// <summary>Top-level composition representing one haemodialysis treatment session.</summary>
    public const string HaemodialysisSession = "openEHR-EHR-COMPOSITION.haemodialysis_session.v1";

    /// <summary>Adverse event observed during a dialysis session (hypotension, cramps, etc.).</summary>
    public const string AdverseEvent = "openEHR-EHR-EVALUATION.adverse_event.v0";
}

/// <summary>Phase of the haemodialysis_session composition emitted as the openEHR projection.</summary>
public enum HaemodialysisSessionPhase
{
    Started = 1,
    Completed = 2,
    Aborted = 3,
}
