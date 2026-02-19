namespace Dialysis.Patient.Application.Features.GetPatients;

public sealed record GetPatientsResponse(IReadOnlyList<PatientSummary> Patients);

public sealed record PatientSummary(
    string Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Gender);
