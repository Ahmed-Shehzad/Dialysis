using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.RegisterProvider;

public sealed record RegisterProviderCommand(
    string NationalProviderIdentifier,
    string FamilyName,
    string GivenName,
    ProviderKind Kind,
    string? SpecialtyCode,
    string? LicenseNumber)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PatientRegister;
}
