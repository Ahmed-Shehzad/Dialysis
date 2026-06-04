using Dialysis.CQRS.Commands;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Consent.Features.GrantConsent;

public sealed record GrantConsentCommand : ICommand<Guid>, IPermissionedCommand
{
    public GrantConsentCommand(Guid PatientId,
        string PartnerId,
        string Scope,
        ConsentDirection Direction,
        DateTime EffectiveFromUtc,
        DateTime? EffectiveToUtc)
    {
        this.PatientId = PatientId;
        this.PartnerId = PartnerId;
        this.Scope = Scope;
        this.Direction = Direction;
        this.EffectiveFromUtc = EffectiveFromUtc;
        this.EffectiveToUtc = EffectiveToUtc;
    }
    public string RequiredPermission => HiePermissions.ConsentManage;
    public Guid PatientId { get; init; }
    public string PartnerId { get; init; }
    public string Scope { get; init; }
    public ConsentDirection Direction { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public DateTime? EffectiveToUtc { get; init; }
    public void Deconstruct(out Guid PatientId, out string PartnerId, out string Scope, out ConsentDirection Direction, out DateTime EffectiveFromUtc, out DateTime? EffectiveToUtc)
    {
        PatientId = this.PatientId;
        PartnerId = this.PartnerId;
        Scope = this.Scope;
        Direction = this.Direction;
        EffectiveFromUtc = this.EffectiveFromUtc;
        EffectiveToUtc = this.EffectiveToUtc;
    }
}
