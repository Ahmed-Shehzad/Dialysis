using Dialysis.CQRS.Commands;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.AssignChair;

public sealed class AssignChairCommandHandler(IPatientQueueRepository repository)
    : ICommandHandler<AssignChairCommand, Guid>
{
    public Task<Guid> HandleAsync(AssignChairCommand request, CancellationToken cancellationToken)
    {
        var entry = repository.Get(request.EntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (repository.IsChairOccupied(today, request.Chair))
            throw new InvalidOperationException($"{request.Chair} is already in use.");
        entry.AssignChair(request.Chair);
        return Task.FromResult(entry.Id);
    }
}
