namespace Dialysis.Gateway.Features.Patients;

public sealed record PatientResponse(
    string LogicalId,
    string? FamilyName,
    string? GivenNames,
    DateTime? BirthDate);
