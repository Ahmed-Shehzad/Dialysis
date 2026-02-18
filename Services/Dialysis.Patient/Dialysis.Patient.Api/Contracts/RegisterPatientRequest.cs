namespace Dialysis.Patient.Api.Contracts;

public sealed record RegisterPatientRequest(
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Gender);
