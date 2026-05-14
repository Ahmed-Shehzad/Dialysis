using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.CodeSets;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Projections;

namespace Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;

public sealed class CompleteSessionCommandHandler(
    IDialysisSessionRepository sessions,
    IUnitOfWork unitOfWork,
    ITransponderBus bus,
    HaemodialysisSessionOpenEhrProjector openEhrProjector,
    TimeProvider timeProvider)
    : ICommandHandler<CompleteSessionCommand, Unit>
{
    public async Task<Unit> HandleAsync(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        var completedAt = timeProvider.GetUtcNow().UtcDateTime;
        session.Complete(completedAt, request.AchievedUfVolumeLiters);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var projection = openEhrProjector.Project(session, HaemodialysisSessionPhase.Completed, completedAt);
        await bus.PublishAsync(new HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: completedAt,
            SchemaVersion: 1,
            SessionId: session.Id,
            PatientId: session.PatientId,
            Phase: HaemodialysisSessionPhase.Completed,
            ArchetypeId: projection.ArchetypeId,
            CompositionJson: projection.CompositionJson,
            PhaseAtUtc: completedAt), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
