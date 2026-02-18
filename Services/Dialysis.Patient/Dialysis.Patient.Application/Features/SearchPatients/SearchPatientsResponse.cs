namespace Dialysis.Patient.Application.Features.SearchPatients;

public sealed record SearchPatientsResponse(IReadOnlyList<PatientMatch> Matches);

public sealed record PatientMatch(
    string Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth);
