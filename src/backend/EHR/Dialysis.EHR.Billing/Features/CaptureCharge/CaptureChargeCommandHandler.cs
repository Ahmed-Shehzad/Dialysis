using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.ChargeEdits;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Features.CaptureCharge;

public sealed class CaptureChargeCommandHandler : ICommandHandler<CaptureChargeCommand, Guid>
{
    private readonly IChargeRepository _charges;
    private readonly IChargeEditChecker _editChecker;
    private readonly IUnitOfWork _unitOfWork;
    public CaptureChargeCommandHandler(IChargeRepository charges,
        IChargeEditChecker editChecker,
        IUnitOfWork unitOfWork)
    {
        _charges = charges;
        _editChecker = editChecker;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(CaptureChargeCommand request, CancellationToken cancellationToken)
    {
        var edits = await _editChecker.CheckChargeAsync(
            request.PatientId, request.CptCode, request.DiagnosisPointerIcd10Codes, payerCode: null, cancellationToken)
            .ConfigureAwait(false);

        if (edits.HasBlocking && !(request.AcknowledgeAdvisories && !string.IsNullOrWhiteSpace(request.OverrideReason)))
            throw new ChargeEditBlockedException(edits.Advisories);

        var overrode = edits.HasBlocking && request.AcknowledgeAdvisories;

        var amount = new Money(request.BilledAmount, request.CurrencyCode);
        var id = Guid.CreateVersion7();
        var charge = Charge.Capture(
            id,
            request.PatientId,
            request.EncounterId,
            request.CptCode,
            request.DiagnosisPointerIcd10Codes,
            amount,
            overrideReason: overrode ? request.OverrideReason : null,
            overriddenBy: overrode ? request.OverriddenBy : null);
        _charges.Add(charge);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
