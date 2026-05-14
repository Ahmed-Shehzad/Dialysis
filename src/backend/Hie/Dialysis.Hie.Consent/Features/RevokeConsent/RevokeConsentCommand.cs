using Dialysis.CQRS.Commands;
using Dialysis.Hie.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Hie.Consent.Features.RevokeConsent;

public sealed record RevokeConsentCommand(Guid ConsentId) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.ConsentManage;
}
