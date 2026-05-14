using Dialysis.CQRS.Commands;
using Dialysis.Hie.Consent.Domain;
using Dialysis.Hie.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Hie.Consent.Features.GrantConsent;

public sealed record GrantConsentCommand(
    Guid PatientId,
    string PartnerId,
    string Scope,
    ConsentDirection Direction,
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.ConsentManage;
}
