namespace Dialysis.Gateway.Features.Patients;

public sealed record CreatePatientRequest(
    string LogicalId,
    string? FamilyName,
    string? GivenNames,
    DateTime? BirthDate);
