namespace Dialysis.HIE.OpenEhr.Archetypes;

/// <summary>
/// Canonical openEHR archetype ids used by the HIE composition store.
/// The text behind each id follows the openEHR CKM naming convention so a future EHRbase swap
/// can pull the matching .adl/.opt definitions from the registry by id.
/// </summary>
public static class ArchetypeIds
{
    public const string PatientDemographics = "openEHR-DEMOGRAPHIC-PERSON.person.v1";
    public const string HaemodialysisSession = "openEHR-EHR-COMPOSITION.haemodialysis_session.v1";
    public const string LabTestResult = "openEHR-EHR-OBSERVATION.lab_test_result.v1";
}
