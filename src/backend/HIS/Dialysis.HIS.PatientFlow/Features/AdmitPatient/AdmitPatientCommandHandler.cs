using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed class AdmitPatientCommandHandler(
    IAdmissionRepository admissions,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdmitPatientCommand, Guid>
{
    public async Task<Guid> Handle(AdmitPatientCommand request, CancellationToken cancellationToken)
    {
        var admission = Admission.Admit(request.PatientId, new WardCode(request.WardCode), DateTime.UtcNow);
        admissions.Add(admission);

        foreach (var @event in admission.IntegrationEvents)
        {
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        admission.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return admission.Id;
    }
}
