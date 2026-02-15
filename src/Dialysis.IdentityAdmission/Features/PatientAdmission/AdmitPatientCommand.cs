using Intercessor.Abstractions;

namespace Dialysis.IdentityAdmission.Features.PatientAdmission;

public sealed record AdmitPatientCommand : ICommand<AdmitPatientResult>
{
    public required string Mrn { get; init; }
    public required string FamilyName { get; init; }
    public string? GivenName { get; init; }
    public DateTimeOffset? BirthDate { get; init; }
}
