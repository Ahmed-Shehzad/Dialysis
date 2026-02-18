namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Groups all parameters needed to map a PDQ patient to FHIR Patient.
/// Aligns with Patient service domain and HL7 PID segment.
/// </summary>
public sealed record PatientMappingInput(
    string Mrn,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PersonNumber);
