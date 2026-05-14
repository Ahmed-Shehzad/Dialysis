using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Projections;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

public sealed class IngestLabResultCommandHandler(
    ITransponderBus bus,
    IUnitOfWork unitOfWork,
    LabResultOpenEhrProjector openEhrProjector,
    TimeProvider timeProvider)
    : ICommandHandler<IngestLabResultCommand, Guid>
{
    public async Task<Guid> HandleAsync(IngestLabResultCommand request, CancellationToken cancellationToken)
    {
        var resultId = Guid.CreateVersion7();
        var occurredOn = timeProvider.GetUtcNow().UtcDateTime;

        var integrationEvent = new LabResultReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: occurredOn,
            SchemaVersion: 1,
            LabResultId: resultId,
            LabOrderId: request.LabOrderId,
            PatientId: request.PatientId,
            LoincCode: request.LoincCode,
            ValueText: request.ValueText,
            UnitCode: request.UnitCode,
            ReferenceRangeText: request.ReferenceRangeText,
            AbnormalFlag: request.AbnormalFlagCode,
            ObservedAtUtc: request.ObservedAtUtc);

        await bus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        var projection = openEhrProjector.Project(request, resultId);
        var openEhrEvent = new LabResultProjectedAsOpenEhrIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: occurredOn,
            SchemaVersion: 1,
            LabResultId: resultId,
            LabOrderId: request.LabOrderId,
            PatientId: request.PatientId,
            ArchetypeId: projection.ArchetypeId,
            CompositionJson: projection.CompositionJson,
            ObservedAtUtc: request.ObservedAtUtc);
        await bus.PublishAsync(openEhrEvent, cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return resultId;
    }
}
