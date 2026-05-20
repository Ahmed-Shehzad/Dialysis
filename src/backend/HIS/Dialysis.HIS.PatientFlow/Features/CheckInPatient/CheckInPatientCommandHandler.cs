using Dialysis.CQRS.Commands;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.CheckInPatient;

public sealed class CheckInPatientCommandHandler(IPatientQueueRepository repository)
    : ICommandHandler<CheckInPatientCommand, Guid>
{
    public Task<Guid> HandleAsync(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        var entry = repository.Get(request.EntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");
        entry.CheckIn(request.EligibilityAcknowledged);
        // ArrivalTimeUtc is captured for an eventual audit/event; the in-memory store
        // doesn't persist it as a separate field today. Acknowledged for future use.
        _ = request.ArrivalTimeUtc;
        return Task.FromResult(entry.Id);
    }
}
