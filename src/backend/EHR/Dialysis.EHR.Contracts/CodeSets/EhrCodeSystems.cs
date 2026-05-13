namespace Dialysis.EHR.Contracts.CodeSets;

/// <summary>
/// Stable identifiers for the standard clinical code systems referenced across the EHR.
/// Used as <c>system</c> on coded value objects so payloads can carry FHIR-compatible Coding pairs.
/// </summary>
public static class EhrCodeSystems
{
    public const string Icd10Cm = "http://hl7.org/fhir/sid/icd-10-cm";
    public const string Icd10Pcs = "http://hl7.org/fhir/sid/icd-10";
    public const string Cpt = "http://www.ama-assn.org/go/cpt";
    public const string Hcpcs = "https://www.cms.gov/Medicare/Coding/HCPCSReleaseCodeSets";
    public const string Loinc = "http://loinc.org";
    public const string SnomedCt = "http://snomed.info/sct";
    public const string Rxnorm = "http://www.nlm.nih.gov/research/umls/rxnorm";
    public const string Ndc = "http://hl7.org/fhir/sid/ndc";
    public const string Cvx = "http://hl7.org/fhir/sid/cvx";
}

/// <summary>Encounter type codes (FHIR v3 ActCode subset).</summary>
public static class EhrEncounterClasses
{
    public const string Ambulatory = "AMB";
    public const string Emergency = "EMER";
    public const string Inpatient = "IMP";
    public const string Home = "HH";
    public const string Virtual = "VR";
}

/// <summary>Billing claim formats handed off to clearinghouses / payers.</summary>
public static class EhrClaimFormats
{
    /// <summary>HIPAA EDI 837 Professional.</summary>
    public const string Edi837Professional = "X12-837P";

    /// <summary>HIPAA EDI 837 Institutional.</summary>
    public const string Edi837Institutional = "X12-837I";

    /// <summary>Paper CMS-1500 fallback.</summary>
    public const string Cms1500 = "CMS-1500";

    /// <summary>Paper UB-04 fallback.</summary>
    public const string Ub04 = "UB-04";
}

/// <summary>E-prescribing transmission formats (NCPDP SCRIPT family).</summary>
public static class EhrPrescriptionFormats
{
    public const string NcpdpScriptNewRx = "NCPDP-SCRIPT-NEWRX";
    public const string NcpdpScriptCancelRx = "NCPDP-SCRIPT-CANCELRX";
    public const string NcpdpScriptRefillRequest = "NCPDP-SCRIPT-REFREQ";
    public const string NcpdpScriptRxRenewal = "NCPDP-SCRIPT-RXRENEWAL";
    public const string NcpdpScriptRxChange = "NCPDP-SCRIPT-RXCHG";
}

/// <summary>HL7 v2 / FHIR lab message archetypes.</summary>
public static class EhrLabFormats
{
    /// <summary>HL7 v2 order message.</summary>
    public const string Hl7V2Orm = "HL7v2-ORM";

    /// <summary>HL7 v2 observation result.</summary>
    public const string Hl7V2Oru = "HL7v2-ORU";

    /// <summary>FHIR ServiceRequest resource.</summary>
    public const string FhirServiceRequest = "FHIR-ServiceRequest";

    /// <summary>FHIR Observation resource.</summary>
    public const string FhirObservation = "FHIR-Observation";
}
