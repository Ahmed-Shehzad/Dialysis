using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.SearchPatients;

public sealed record PatientSummary(
    Guid Id,
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    DateOnly DateOfBirth,
    string? SexAtBirthCode,
    string Status);

public sealed record PatientSearchResult(
    IReadOnlyList<PatientSummary> Items,
    int TotalCount,
    int Skip,
    int Take);

public sealed record SearchPatientsQuery(
    string? Query = null,
    string? FamilyName = null,
    string? GivenName = null,
    string? MedicalRecordNumber = null,
    DateOnly? DateOfBirthFrom = null,
    DateOnly? DateOfBirthTo = null,
    string? SexAtBirthCode = null,
    PatientStatus? Status = null,
    int Skip = 0,
    int Take = 25)
    : IQuery<PatientSearchResult>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientRead;
}
