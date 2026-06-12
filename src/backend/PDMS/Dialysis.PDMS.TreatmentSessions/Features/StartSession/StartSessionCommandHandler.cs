using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.CodeSets;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.StartSession;

public sealed class StartSessionCommandHandler : ICommandHandler<StartSessionCommand, Unit>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public StartSessionCommandHandler(IDialysisSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        var startedAt = _timeProvider.GetUtcNow().UtcDateTime;
        session.Start(startedAt);
        // The aggregate raises the openEHR projection event; the SaveChanges interceptor drains it
        // into the Transponder outbox atomically with the phase transition — no manual bus publishing.
        var projection = HaemodialysisSessionOpenEhrProjector.Project(session, HaemodialysisSessionPhase.Started, startedAt);
        session.RecordOpenEhrProjection(HaemodialysisSessionPhase.Started, projection.ArchetypeId, projection.CompositionJson, startedAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
