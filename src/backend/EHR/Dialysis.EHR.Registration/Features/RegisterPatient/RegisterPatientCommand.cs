using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.RegisterPatient;

public sealed record RegisterPatientCommand(
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string? SexAtBirthCode,
    string? PreferredLanguageCode)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientRegister;
}
