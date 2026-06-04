using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Dialysis.EHR.PatientChart.Projections;

namespace Dialysis.EHR.PatientChart.Features.RecordVitalSign;

public sealed class RecordVitalSignCommandHandler : ICommandHandler<RecordVitalSignCommand, Guid>
{
    private readonly IVitalSignRepository _vitals;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransponderBus _bus;
    private readonly VitalSignOpenEhrProjector _openEhrProjector;
    private readonly TimeProvider _timeProvider;
    public RecordVitalSignCommandHandler(IVitalSignRepository vitals,
        IUnitOfWork unitOfWork,
        ITransponderBus bus,
        VitalSignOpenEhrProjector openEhrProjector,
        TimeProvider timeProvider)
    {
        _vitals = vitals;
        _unitOfWork = unitOfWork;
        _bus = bus;
        _openEhrProjector = openEhrProjector;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(RecordVitalSignCommand request, CancellationToken cancellationToken)
    {
        var observation = new Coding(EhrCodeSystems.Loinc, request.LoincCode, request.Display);
        var id = request.ReadingId != Guid.Empty ? request.ReadingId : Guid.CreateVersion7();
        var reading = VitalSignReading.Record(
            id,
            request.PatientId,
            observation,
            request.Value,
            request.UnitCode,
            request.ObservedAtUtc,
            request.EncounterId,
            request.RecordedByProviderId);
        _vitals.Add(reading);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_openEhrProjector.Project(reading) is { } projection)
        {
            var openEhrEvent = new ChartVitalSignProjectedAsOpenEhrIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: _timeProvider.GetUtcNow().UtcDateTime,
                SchemaVersion: 1,
                VitalSignReadingId: id,
                PatientId: reading.PatientId,
                EncounterId: reading.EncounterId,
                RecordedByProviderId: reading.RecordedByProviderId,
                ArchetypeId: projection.ArchetypeId,
                CompositionJson: projection.CompositionJson,
                ObservedAtUtc: reading.ObservedAtUtc);
            await _bus.PublishAsync(openEhrEvent, cancellationToken).ConfigureAwait(false);
        }

        return id;
    }
}
