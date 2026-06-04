using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed class AdmitPatientCommandHandler : ICommandHandler<AdmitPatientCommand, Guid>
{
    private readonly IAdmissionRepository _admissions;
    private readonly ITransponderOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    public AdmitPatientCommandHandler(IAdmissionRepository admissions,
        ITransponderOutbox outbox,
        IUnitOfWork unitOfWork)
    {
        _admissions = admissions;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(AdmitPatientCommand request, CancellationToken cancellationToken)
    {
        var admission = Admission.Admit(request.PatientId, new WardCode(request.WardCode), DateTime.UtcNow);
        _admissions.Add(admission);

        foreach (var @event in admission.IntegrationEvents)
        {
            await _outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        admission.ClearIntegrationEvents();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return admission.Id;
    }
}
