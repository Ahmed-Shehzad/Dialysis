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
        string ClaimFormatCode = EhrClaimFormats.Edi837Professional,
        bool AcknowledgeAdvisories = false,
        string? OverrideReason = null,
        string? OverriddenBy = null)
    {
        this.PatientId = PatientId;
        this.PayerId = PayerId;
        this.ChargeIds = ChargeIds;
        this.ClaimFormatCode = ClaimFormatCode;
        this.AcknowledgeAdvisories = AcknowledgeAdvisories;
        this.OverrideReason = OverrideReason;
        this.OverriddenBy = OverriddenBy;
    }
    public string RequiredPermission => EhrPermissions.ClaimSubmit;
    public Guid PatientId { get; init; }
    public Guid PayerId { get; init; }
    public IReadOnlyList<Guid> ChargeIds { get; init; }
    public string ClaimFormatCode { get; init; }

    /// <summary>When true, blocking charge-review edits on the claim's charges are overridden (requires a reason).</summary>
    public bool AcknowledgeAdvisories { get; init; }

    /// <summary>The biller's reason for overriding a blocking edit at submission; audited.</summary>
    public string? OverrideReason { get; init; }

    /// <summary>Server-populated identity of the overriding biller.</summary>
    public string? OverriddenBy { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid PayerId, out IReadOnlyList<Guid> ChargeIds, out string ClaimFormatCode)
    {
        PatientId = this.PatientId;
        PayerId = this.PayerId;
        ChargeIds = this.ChargeIds;
        ClaimFormatCode = this.ClaimFormatCode;
    }
}
