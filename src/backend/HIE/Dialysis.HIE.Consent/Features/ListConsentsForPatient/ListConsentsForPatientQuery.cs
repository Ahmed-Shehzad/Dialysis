using Dialysis.CQRS.Queries;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Consent.Features.ListConsentsForPatient;

public sealed record ListConsentsForPatientQuery : IQuery<IReadOnlyList<ConsentDto>>, IPermissionedCommand
{
    public ListConsentsForPatientQuery(Guid PatientId) => this.PatientId = PatientId;
    public string RequiredPermission => HiePermissions.ConsentManage;
    public Guid PatientId { get; init; }
    public void Deconstruct(out Guid patientId) => patientId = this.PatientId;
}

public sealed record ConsentDto
{
    public ConsentDto(Guid Id,
        Guid PatientId,
        string PartnerId,
        string Scope,
        ConsentDirection Direction,
        DateTime EffectiveFromUtc,
        DateTime? EffectiveToUtc,
        DateTime? RevokedAtUtc)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.PartnerId = PartnerId;
        this.Scope = Scope;
        this.Direction = Direction;
        this.EffectiveFromUtc = EffectiveFromUtc;
        this.EffectiveToUtc = EffectiveToUtc;
        this.RevokedAtUtc = RevokedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PartnerId { get; init; }
    public string Scope { get; init; }
    public ConsentDirection Direction { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public DateTime? EffectiveToUtc { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
    public void Deconstruct(out Guid id, out Guid patientId, out string partnerId, out string scope, out ConsentDirection direction, out DateTime effectiveFromUtc, out DateTime? effectiveToUtc, out DateTime? revokedAtUtc)
    {
        id = this.Id;
        patientId = this.PatientId;
        partnerId = this.PartnerId;
        scope = this.Scope;
        direction = this.Direction;
        effectiveFromUtc = this.EffectiveFromUtc;
        effectiveToUtc = this.EffectiveToUtc;
        revokedAtUtc = this.RevokedAtUtc;
    }
}
