namespace Dialysis.HIE.Core.Coding;

/// <summary>
/// Canonical URIs for the coding systems the HIE emits in FHIR <c>Coding.system</c>. The system
/// URIs are forever-stable identifiers — they identify the system, not the codes inside it — so
/// keeping them as hardcoded constants is correct and cheap.
///
/// Specific concept codes (e.g. SNOMED <c>265764009</c> "Renal dialysis") do NOT live here. They
/// are catalogued in <see cref="ClinicalConcepts"/> and looked up / validated at runtime through
/// <c>Dialysis.BuildingBlocks.Fhir.Terminology.ITerminologyService</c>.
/// </summary>
public static class CodeSystems
{
    public const string SnomedCt = "http://snomed.info/sct";
    public const string Loinc = "http://loinc.org";
    public const string Icd10 = "http://hl7.org/fhir/sid/icd-10";
    public const string Cpt = "http://www.ama-assn.org/go/cpt";
    public const string Ucum = "http://unitsofmeasure.org";
    public const string AdministrativeGender = "http://hl7.org/fhir/administrative-gender";
    public const string V3Race = "http://terminology.hl7.org/CodeSystem/v3-Race";
    public const string V3Ethnicity = "http://terminology.hl7.org/CodeSystem/v3-Ethnicity";

    public const string MrnIdentifier = "http://terminology.hl7.org/CodeSystem/v2-0203";
}
