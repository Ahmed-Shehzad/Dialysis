using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.RecordRemittance;

public sealed class RecordRemittanceCommandHandler : ICommandHandler<RecordRemittanceCommand, Guid>
{
    private readonly IClaimRepository _claims;
    private readonly IRemittanceRepository _remittances;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public RecordRemittanceCommandHandler(IClaimRepository claims,
        IRemittanceRepository remittances,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _claims = claims;
        _remittances = remittances;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(RecordRemittanceCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claims.GetAsync(request.ClaimId, cancellationToken).ConfigureAwait(false)
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
            _timeProvider.GetUtcNow().UtcDateTime);

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

        _remittances.Add(remittance);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
