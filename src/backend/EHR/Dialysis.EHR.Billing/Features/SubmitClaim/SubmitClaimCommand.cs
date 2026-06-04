using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.SubmitClaim;

public sealed record SubmitClaimCommand : ICommand<Guid>, IPermissionedCommand
{
    public SubmitClaimCommand(Guid PatientId,
        Guid PayerId,
        IReadOnlyList<Guid> ChargeIds,
        string ClaimFormatCode = EhrClaimFormats.Edi837Professional)
    {
        this.PatientId = PatientId;
        this.PayerId = PayerId;
        this.ChargeIds = ChargeIds;
        this.ClaimFormatCode = ClaimFormatCode;
    }
    public string RequiredPermission => EhrPermissions.ClaimSubmit;
    public Guid PatientId { get; init; }
    public Guid PayerId { get; init; }
    public IReadOnlyList<Guid> ChargeIds { get; init; }
    public string ClaimFormatCode { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid PayerId, out IReadOnlyList<Guid> ChargeIds, out string ClaimFormatCode)
    {
        PatientId = this.PatientId;
        PayerId = this.PayerId;
        ChargeIds = this.ChargeIds;
        ClaimFormatCode = this.ClaimFormatCode;
    }
}
