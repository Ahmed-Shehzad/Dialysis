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
        string? OverriddenBy = null,
        InstitutionalClaimRequest? Institutional = null)
    {
        this.PatientId = PatientId;
        this.PayerId = PayerId;
        this.ChargeIds = ChargeIds;
        this.ClaimFormatCode = ClaimFormatCode;
        this.AcknowledgeAdvisories = AcknowledgeAdvisories;
        this.OverrideReason = OverrideReason;
        this.OverriddenBy = OverriddenBy;
        this.Institutional = Institutional;
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

    /// <summary>
    /// Institutional (837I / UB-04) claim data; required when <see cref="ClaimFormatCode"/> is an
    /// institutional format, ignored-as-invalid otherwise. Professional claims leave this null.
    /// </summary>
    public InstitutionalClaimRequest? Institutional { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid PayerId, out IReadOnlyList<Guid> ChargeIds, out string ClaimFormatCode)
    {
        PatientId = this.PatientId;
        PayerId = this.PayerId;
        ChargeIds = this.ChargeIds;
        ClaimFormatCode = this.ClaimFormatCode;
    }
}

/// <summary>
/// Institutional (837I / UB-04) section of a <see cref="SubmitClaimCommand"/> — the UB-04 type of
/// bill (four characters including the leading zero, e.g. <c>0721</c> ESRD freestanding), the
/// statement-covers period, the optional admission date/type, and the optional ICD-10-PCS
/// procedure codes (principal + others).
/// </summary>
public sealed record InstitutionalClaimRequest(
    string TypeOfBill,
    DateTime StatementFromUtc,
    DateTime StatementToUtc,
    DateTime? AdmissionDateUtc = null,
    string? AdmissionTypeCode = null,
    string? PrincipalProcedureCode = null,
    IReadOnlyList<string>? OtherProcedureCodes = null);
