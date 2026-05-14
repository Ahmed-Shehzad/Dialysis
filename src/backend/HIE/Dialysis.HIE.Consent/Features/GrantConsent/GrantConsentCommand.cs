using Dialysis.CQRS.Commands;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Consent.Features.GrantConsent;

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
