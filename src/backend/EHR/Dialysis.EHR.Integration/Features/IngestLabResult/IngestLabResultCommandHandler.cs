using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

public sealed class IngestLabResultCommandHandler(
    ITransponderBus bus,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<IngestLabResultCommand, Guid>
{
    public async Task<Guid> HandleAsync(IngestLabResultCommand request, CancellationToken cancellationToken)
    {
        var resultId = Guid.CreateVersion7();
        var integrationEvent = new LabResultReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: timeProvider.GetUtcNow().UtcDateTime,
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
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return resultId;
    }
}
