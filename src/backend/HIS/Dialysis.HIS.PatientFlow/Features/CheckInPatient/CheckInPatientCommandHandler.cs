using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.CheckInPatient;

public sealed class CheckInPatientCommandHandler : ICommandHandler<CheckInPatientCommand, Guid>
{
    private readonly IPatientQueueRepository _repository;
    private readonly ITransponderOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    public CheckInPatientCommandHandler(IPatientQueueRepository repository,
        ITransponderOutbox outbox,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        var entry = _repository.Get(request.EntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");
        entry.CheckIn(request.ArrivalTimeUtc, request.EligibilityAcknowledged);

        foreach (var @event in entry.IntegrationEvents)
        {
            await _outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        entry.ClearIntegrationEvents();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
