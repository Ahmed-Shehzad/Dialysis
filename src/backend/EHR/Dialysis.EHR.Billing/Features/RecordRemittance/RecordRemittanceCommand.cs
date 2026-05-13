using Dialysis.CQRS.Commands;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.RecordRemittance;

public sealed record RecordRemittanceCommand(
    Guid ClaimId,
    string PayerCode,
    decimal PaidAmount,
    decimal AdjustmentAmount,
    string CurrencyCode,
    AdjudicationStatus AdjudicationStatus)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.PaymentPost;
}
