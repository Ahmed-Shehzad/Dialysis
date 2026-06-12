using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.CodeSets;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.AbortSession;

public sealed class AbortSessionCommandHandler : ICommandHandler<AbortSessionCommand, Unit>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public AbortSessionCommandHandler(IDialysisSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(AbortSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        var abortedAt = _timeProvider.GetUtcNow().UtcDateTime;
        session.Abort(abortedAt, request.ReasonCode);
        // The aggregate raises the openEHR projection event; the SaveChanges interceptor drains it
        // into the Transponder outbox atomically with the phase transition — no manual bus publishing.
        var projection = HaemodialysisSessionOpenEhrProjector.Project(session, HaemodialysisSessionPhase.Aborted, abortedAt);
        session.RecordOpenEhrProjection(HaemodialysisSessionPhase.Aborted, projection.ArchetypeId, projection.CompositionJson, abortedAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
