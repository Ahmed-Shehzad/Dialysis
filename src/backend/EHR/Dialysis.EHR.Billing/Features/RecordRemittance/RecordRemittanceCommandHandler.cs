using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.RecordRemittance;

public sealed class RecordRemittanceCommandHandler(
    IClaimRepository claims,
    IRemittanceRepository remittances,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RecordRemittanceCommand, Guid>
{
    public async Task<Guid> Handle(RecordRemittanceCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAsync(request.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Claim '{request.ClaimId}' not found.");

        var paid = new Money(request.PaidAmount, request.CurrencyCode);
        var adjustment = new Money(request.AdjustmentAmount, request.CurrencyCode);
        var id = Guid.CreateVersion7();
        var remittance = Remittance.Record(
            id,
            claim.Id,
            request.PayerCode,
            paid,
            adjustment,
            request.AdjudicationStatus,
            timeProvider.GetUtcNow().UtcDateTime);

        switch (request.AdjudicationStatus)
        {
            case AdjudicationStatus.Paid:
                claim.MarkPaid();
                break;
            case AdjudicationStatus.PartiallyPaid:
                claim.MarkPartiallyPaid();
                break;
            case AdjudicationStatus.Denied:
                claim.MarkDenied();
                break;
        }

        remittances.Add(remittance);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
