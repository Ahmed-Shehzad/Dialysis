using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Projections;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

public sealed class IngestLabResultCommandHandler : ICommandHandler<IngestLabResultCommand, Guid>
{
    private readonly ITransponderBus _bus;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public IngestLabResultCommandHandler(ITransponderBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _bus = bus;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(IngestLabResultCommand request, CancellationToken cancellationToken)
    {
        var resultId = Guid.CreateVersion7();
        var occurredOn = _timeProvider.GetUtcNow().UtcDateTime;

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

        await _bus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        var projection = LabResultOpenEhrProjector.Project(request, resultId);
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
        await _bus.PublishAsync(openEhrEvent, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return resultId;
    }
}
