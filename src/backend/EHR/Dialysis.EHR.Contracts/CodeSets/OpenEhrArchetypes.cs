namespace Dialysis.EHR.Contracts.CodeSets;

/// <summary>
/// Canonical openEHR archetype identifiers EHR projects to. Names follow the openEHR CKM convention
/// so a future EHRbase swap pulls the matching .adl/.opt definitions from the registry by id.
/// </summary>
public static class OpenEhrArchetypes
{
    public const string BloodPressure = "openEHR-EHR-OBSERVATION.blood_pressure.v2";
    public const string BodyWeight = "openEHR-EHR-OBSERVATION.body_weight.v2";
    public const string BodyHeight = "openEHR-EHR-OBSERVATION.height.v2";
    public const string PulseRate = "openEHR-EHR-OBSERVATION.pulse.v2";
    public const string BodyTemperature = "openEHR-EHR-OBSERVATION.body_temperature.v2";
    public const string LabTestResult = "openEHR-EHR-OBSERVATION.lab_test_result.v1";
}

/// <summary>LOINC codes the EHR explicitly projects to openEHR archetypes.</summary>
public static class EhrLoincCodes
{
    public const string BloodPressurePanel = "85354-9";
    public const string SystolicBloodPressure = "8480-6";
    public const string DiastolicBloodPressure = "8462-4";
    public const string BodyWeight = "29463-7";
    public const string BodyHeight = "8302-2";
    public const string PulseRate = "8867-4";
    public const string BodyTemperature = "8310-5";
}
