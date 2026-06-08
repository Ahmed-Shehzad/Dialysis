using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Projections;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

public sealed class IngestLabResultCommandHandler : ICommandHandler<IngestLabResultCommand, Guid>
{
    private readonly ITransponderBus _bus;
    private readonly ILabResultRepository _labResults;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public IngestLabResultCommandHandler(ITransponderBus bus,
        ILabResultRepository labResults,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _bus = bus;
        _labResults = labResults;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(IngestLabResultCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var resultId = Guid.CreateVersion7();
        var occurredOn = _timeProvider.GetUtcNow().UtcDateTime;

        // Persist the chart read-model row so GET /patients/{id}/lab-results surfaces it. Without this
        // the order would stay result-less on the chart (the read model was never written before).
        var labResult = LabResult.Receive(
            resultId,
            request.LabOrderId,
            request.PatientId,
            request.LoincCode,
            request.ValueText,
            MapAbnormalFlag(request.AbnormalFlagCode),
            request.ObservedAtUtc,
            request.UnitCode,
            request.ReferenceRangeText);
        _labResults.Add(labResult);

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

    /// <summary>Maps an HL7 abnormal-flag code (table 0078) to the chart's <see cref="LabAbnormalFlag"/>.</summary>
    private static LabAbnormalFlag MapAbnormalFlag(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "L" => LabAbnormalFlag.Low,
        "H" => LabAbnormalFlag.High,
        "LL" or "HH" or "C" or "CRIT" => LabAbnormalFlag.Critical,
        "A" or "AA" => LabAbnormalFlag.AbnormalNos,
        _ => LabAbnormalFlag.Normal,
    };
}
