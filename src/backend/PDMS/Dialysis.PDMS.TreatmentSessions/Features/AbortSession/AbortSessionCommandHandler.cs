using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.CodeSets;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.AbortSession;

public sealed class AbortSessionCommandHandler(
    IDialysisSessionRepository sessions,
    IUnitOfWork unitOfWork,
    ITransponderBus bus,
    HaemodialysisSessionOpenEhrProjector openEhrProjector,
    TimeProvider timeProvider)
    : ICommandHandler<AbortSessionCommand, Unit>
{
    public async Task<Unit> HandleAsync(AbortSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        var abortedAt = timeProvider.GetUtcNow().UtcDateTime;
        session.Abort(abortedAt, request.ReasonCode);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var projection = openEhrProjector.Project(session, HaemodialysisSessionPhase.Aborted, abortedAt);
        await bus.PublishAsync(new HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: abortedAt,
            SchemaVersion: 1,
            SessionId: session.Id,
            PatientId: session.PatientId,
            Phase: HaemodialysisSessionPhase.Aborted,
            ArchetypeId: projection.ArchetypeId,
            CompositionJson: projection.CompositionJson,
            PhaseAtUtc: abortedAt), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
