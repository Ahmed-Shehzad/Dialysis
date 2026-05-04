using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Integration;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.DischargePatient;

public sealed class DischargePatientCommandHandler(IPatientRepository patients, IUnitOfWork unitOfWork, ITransponderOutbox outbox)
    : ICommandHandler<DischargePatientCommand>
{
    public async Task<Unit> Handle(DischargePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(request.PatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Patient not found.");

        patient.Discharge(DateTime.UtcNow, actorId: null);
        await OutboxFlush.ForAggregateAsync(patient, outbox, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
