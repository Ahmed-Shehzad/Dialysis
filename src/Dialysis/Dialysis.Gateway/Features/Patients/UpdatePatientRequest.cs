namespace Dialysis.Gateway.Features.Patients;

public sealed record UpdatePatientRequest(
    string? FamilyName,
    string? GivenNames,
    DateTime? BirthDate);
