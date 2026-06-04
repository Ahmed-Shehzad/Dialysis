using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;

public sealed class RegisterWalkInCommandHandler : ICommandHandler<RegisterWalkInCommand, PatientQueueEntryDto>
{
    private readonly IPatientQueueRepository _repository;
    private readonly ITransponderOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterWalkInCommandHandler(IPatientQueueRepository repository,
        ITransponderOutbox outbox,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }
    public async Task<PatientQueueEntryDto> HandleAsync(
        RegisterWalkInCommand request,
        CancellationToken cancellationToken)
    {
        var entry = PatientQueueEntry.WalkIn(
            id: Guid.CreateVersion7(),
            patientId: Guid.CreateVersion7(),
            patientName: request.PatientName.Trim(),
            mrn: request.Mrn.Trim(),
            arrivalUtc: DateTime.UtcNow,
            eligibilityVerified: request.EligibilityVerified);
        _repository.Add(entry);

        foreach (var @event in entry.IntegrationEvents)
        {
            await _outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        entry.ClearIntegrationEvents();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PatientQueueEntryDto(
            entry.Id,
            entry.PatientId,
            entry.PatientName,
            entry.Mrn,
            entry.ScheduledForUtc,
            "waiting",
            entry.Chair,
            entry.EligibilityVerified);
    }
}
