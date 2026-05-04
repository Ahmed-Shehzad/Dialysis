using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientFlow.Integration;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed class AdmitPatientCommandHandler(IPatientRepository patients, IUnitOfWork unitOfWork, ITransponderOutbox outbox)
    : ICommandHandler<AdmitPatientCommand>
{
    public async Task<Unit> Handle(AdmitPatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(request.PatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Patient not found.");

        patient.Admit(DateTime.UtcNow, actorId: null);
        await OutboxFlush.ForAggregateAsync(patient, outbox, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
