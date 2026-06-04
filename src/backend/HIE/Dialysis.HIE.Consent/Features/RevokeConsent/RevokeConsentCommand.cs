using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Consent.Features.RevokeConsent;

public sealed record RevokeConsentCommand : ICommand, IPermissionedCommand
{
    public RevokeConsentCommand(Guid ConsentId) => this.ConsentId = ConsentId;
    public string RequiredPermission => HiePermissions.ConsentManage;
    public Guid ConsentId { get; init; }
    public void Deconstruct(out Guid consentId) => consentId = this.ConsentId;
}
