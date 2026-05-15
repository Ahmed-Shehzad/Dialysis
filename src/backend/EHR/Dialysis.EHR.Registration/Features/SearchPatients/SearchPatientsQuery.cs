using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
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

public sealed record SearchPatientsQuery(string? Query, int Take = 25)
    : IQuery<IReadOnlyList<PatientSummary>>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientRead;
}
