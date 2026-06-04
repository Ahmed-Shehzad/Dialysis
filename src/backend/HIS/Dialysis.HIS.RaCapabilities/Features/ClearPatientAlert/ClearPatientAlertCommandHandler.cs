using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;

public sealed class ClearPatientAlertCommandHandler : ICommandHandler<ClearPatientAlertCommand>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public ClearPatientAlertCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(ClearPatientAlertCommand request, CancellationToken cancellationToken)
    {
        var ok = await _store.TryClearPatientAlertAsync(request.AlertId, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        if (!ok)
            throw new InvalidOperationException("Alert not found or already cleared.");
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
