namespace Dialysis.Hie.Core.Coding;

/// <summary>
/// Canonical URIs for the coding systems the HIE emits in FHIR <c>Coding.system</c>. Centralised so mappers
/// reference one constant and a future system upgrade is a single change.
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

    /// <summary>SNOMED CT code for haemodialysis procedure.</summary>
    public const string SnomedHaemodialysisCode = "265764009";
    /// <summary>LOINC code for a clinical-note DocumentReference type.</summary>
    public const string LoincClinicalNoteTypeCode = "11506-3";
}
