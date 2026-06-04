using Dialysis.CQRS.Commands;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.PostPayment;

public sealed record PostPaymentCommand : ICommand<Guid>, IPermissionedCommand
{
    public PostPaymentCommand(Guid PatientId,
        Guid? ClaimId,
        decimal Amount,
        string CurrencyCode,
        PaymentMethod Method,
        string? ExternalReference)
    {
        this.PatientId = PatientId;
        this.ClaimId = ClaimId;
        this.Amount = Amount;
        this.CurrencyCode = CurrencyCode;
        this.Method = Method;
        this.ExternalReference = ExternalReference;
    }
    public string RequiredPermission => EhrPermissions.PaymentPost;
    public Guid PatientId { get; init; }
    public Guid? ClaimId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; }
    public PaymentMethod Method { get; init; }
    public string? ExternalReference { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid? ClaimId, out decimal Amount, out string CurrencyCode, out PaymentMethod Method, out string? ExternalReference)
    {
        PatientId = this.PatientId;
        ClaimId = this.ClaimId;
        Amount = this.Amount;
        CurrencyCode = this.CurrencyCode;
        Method = this.Method;
        ExternalReference = this.ExternalReference;
    }
}
