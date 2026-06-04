using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.CaptureCharge;

public sealed class CaptureChargeCommandHandler : ICommandHandler<CaptureChargeCommand, Guid>
{
    private readonly IChargeRepository _charges;
    private readonly IUnitOfWork _unitOfWork;
    public CaptureChargeCommandHandler(IChargeRepository charges,
        IUnitOfWork unitOfWork)
    {
        _charges = charges;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(CaptureChargeCommand request, CancellationToken cancellationToken)
    {
        var amount = new Money(request.BilledAmount, request.CurrencyCode);
        var id = Guid.CreateVersion7();
        var charge = Charge.Capture(
            id,
            request.PatientId,
            request.EncounterId,
            request.CptCode,
            request.DiagnosisPointerIcd10Codes,
            amount);
        _charges.Add(charge);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
