using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AssignChair;

public sealed class AssignChairCommandHandler(
    IPatientQueueRepository repository,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AssignChairCommand, Guid>
{
    public async Task<Guid> HandleAsync(AssignChairCommand request, CancellationToken cancellationToken)
    {
        var entry = repository.Get(request.EntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (repository.IsChairOccupied(today, request.Chair))
            throw new InvalidOperationException($"{request.Chair} is already in use.");
        entry.AssignChair(request.Chair, DateTime.UtcNow);

        foreach (var @event in entry.IntegrationEvents)
        {
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        entry.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
