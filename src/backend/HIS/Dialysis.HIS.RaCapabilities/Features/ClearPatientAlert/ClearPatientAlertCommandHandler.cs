using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;

public sealed class ClearPatientAlertCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<ClearPatientAlertCommand>
{
    public async Task<Unit> Handle(ClearPatientAlertCommand request, CancellationToken cancellationToken)
    {
        var ok = await store.TryClearPatientAlertAsync(request.AlertId, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        if (!ok)
            throw new InvalidOperationException("Alert not found or already cleared.");
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
