using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.CodeSets;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.StartSession;

public sealed class StartSessionCommandHandler(
    IDialysisSessionRepository sessions,
    IUnitOfWork unitOfWork,
    ITransponderBus bus,
    HaemodialysisSessionOpenEhrProjector openEhrProjector,
    TimeProvider timeProvider)
    : ICommandHandler<StartSessionCommand, Unit>
{
    public async Task<Unit> HandleAsync(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        var startedAt = timeProvider.GetUtcNow().UtcDateTime;
        session.Start(startedAt);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var projection = openEhrProjector.Project(session, HaemodialysisSessionPhase.Started, startedAt);
        await bus.PublishAsync(new HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: startedAt,
            SchemaVersion: 1,
            SessionId: session.Id,
            PatientId: session.PatientId,
            Phase: HaemodialysisSessionPhase.Started,
            ArchetypeId: projection.ArchetypeId,
            CompositionJson: projection.CompositionJson,
            PhaseAtUtc: startedAt), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
