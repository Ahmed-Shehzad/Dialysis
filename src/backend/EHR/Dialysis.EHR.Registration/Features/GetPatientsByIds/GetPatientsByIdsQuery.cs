using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.GetPatientsByIds;

/// <summary>
/// A slim, label-only projection of a patient: just what a UI needs to render a readable
/// "name · MRN" wherever a patient id appears. Deliberately omits sex / language / status (data
/// minimisation) — callers needing the full identity surface use <c>GET /patients/{id}</c>.
/// </summary>
public sealed record PatientLabelDto
{
    /// <summary>Creates the label projection.</summary>
    public PatientLabelDto(Guid Id, string MedicalRecordNumber, string FamilyName, string GivenName,
        string? MiddleName, DateOnly DateOfBirth)
    {
        this.Id = Id;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.FamilyName = FamilyName;
        this.GivenName = GivenName;
        this.MiddleName = MiddleName;
        this.DateOfBirth = DateOfBirth;
    }
    public Guid Id { get; init; }
    public string MedicalRecordNumber { get; init; }
    public string FamilyName { get; init; }
    public string GivenName { get; init; }
    public string? MiddleName { get; init; }
    public DateOnly DateOfBirth { get; init; }
}

/// <summary>
/// Resolves many patient ids to their <see cref="PatientLabelDto"/> in one round-trip, so a list page
/// with N rows can label every row without an N+1 of single reads. Gated on the same
/// <see cref="EhrPermissions.PatientRead"/> permission as the single fetch.
/// </summary>
public sealed record GetPatientsByIdsQuery : IQuery<IReadOnlyList<PatientLabelDto>>, IPermissionedCommand
{
    /// <summary>Creates the query.</summary>
    public GetPatientsByIdsQuery(IReadOnlyCollection<Guid> PatientIds) => this.PatientIds = PatientIds;
    public string RequiredPermission => EhrPermissions.PatientRead;
    public IReadOnlyCollection<Guid> PatientIds { get; init; }
}
