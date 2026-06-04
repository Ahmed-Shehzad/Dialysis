using Dialysis.CQRS.Commands;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.RecordRemittance;

public sealed record RecordRemittanceCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordRemittanceCommand(Guid ClaimId,
        string PayerCode,
        decimal PaidAmount,
        decimal AdjustmentAmount,
        string CurrencyCode,
        AdjudicationStatus AdjudicationStatus)
    {
        this.ClaimId = ClaimId;
        this.PayerCode = PayerCode;
        this.PaidAmount = PaidAmount;
        this.AdjustmentAmount = AdjustmentAmount;
        this.CurrencyCode = CurrencyCode;
        this.AdjudicationStatus = AdjudicationStatus;
    }
    public string RequiredPermission => EhrPermissions.PaymentPost;
    public Guid ClaimId { get; init; }
    public string PayerCode { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal AdjustmentAmount { get; init; }
    public string CurrencyCode { get; init; }
    public AdjudicationStatus AdjudicationStatus { get; init; }
    public void Deconstruct(out Guid ClaimId, out string PayerCode, out decimal PaidAmount, out decimal AdjustmentAmount, out string CurrencyCode, out AdjudicationStatus AdjudicationStatus)
    {
        ClaimId = this.ClaimId;
        PayerCode = this.PayerCode;
        PaidAmount = this.PaidAmount;
        AdjustmentAmount = this.AdjustmentAmount;
        CurrencyCode = this.CurrencyCode;
        AdjudicationStatus = this.AdjudicationStatus;
    }
}
