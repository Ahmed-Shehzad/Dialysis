namespace Dialysis.Patient.Application.Features.GetPatientByMrn;

/// <summary>
/// Patient demographics response - aligns with PDQ PID segment.
/// </summary>
public sealed record GetPatientByMrnResponse(
    string Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Gender);
