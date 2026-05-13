using Dialysis.CQRS.Commands;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.PostPayment;

public sealed record PostPaymentCommand(
    Guid PatientId,
    Guid? ClaimId,
    decimal Amount,
    string CurrencyCode,
    PaymentMethod Method,
    string? ExternalReference)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PaymentPost;
}
