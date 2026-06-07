using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordAdverseEvent;

public sealed class RecordAdverseEventCommandHandler : ICommandHandler<RecordAdverseEventCommand, Unit>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly ITransponderBus _bus;
    private readonly TimeProvider _timeProvider;
    public RecordAdverseEventCommandHandler(IDialysisSessionRepository sessions,
        ITransponderBus bus,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _bus = bus;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(RecordAdverseEventCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Session '{request.SessionId}' not found.");
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await _bus.PublishAsync(new IntradialyticAdverseEventIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: now,
            SchemaVersion: 1,
            SessionId: session.Id,
            PatientId: session.PatientId,
            ObservedAtUtc: now,
            EventKindCode: request.EventKindCode,
            Severity: request.Severity,
            Notes: request.Notes), cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
