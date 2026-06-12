using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordAdverseEvent;

public sealed class RecordAdverseEventCommandHandler : ICommandHandler<RecordAdverseEventCommand, Unit>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public RecordAdverseEventCommandHandler(IDialysisSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(RecordAdverseEventCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Session '{request.SessionId}' not found.");

        // The aggregate raises the adverse-event integration event; the SaveChanges interceptor
        // drains it into the Transponder outbox transactionally — no manual bus publishing.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        session.RecordAdverseEvent(now, request.EventKindCode, request.Severity, request.Notes);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
