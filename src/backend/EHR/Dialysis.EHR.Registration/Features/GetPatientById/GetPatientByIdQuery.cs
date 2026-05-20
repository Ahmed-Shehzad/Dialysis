using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.GetPatientById;

/// <summary>
/// Identity surface for one patient. Sized to what every consumer of the chart header
/// needs (display name, MRN, demographics, status) without dragging clinical sections in;
/// callers wanting allergies / problems / meds keep using <c>GET /patients/{id}/chart</c>.
/// </summary>
public sealed record PatientDetailDto(
    Guid Id,
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string? SexAtBirthCode,
    string? PreferredLanguageCode,
    string Status);

public sealed record GetPatientByIdQuery(Guid PatientId)
    : IQuery<PatientDetailDto?>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientRead;
}
