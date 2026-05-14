using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Consent.Features.RevokeConsent;

public sealed record RevokeConsentCommand(Guid ConsentId) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.ConsentManage;
}
