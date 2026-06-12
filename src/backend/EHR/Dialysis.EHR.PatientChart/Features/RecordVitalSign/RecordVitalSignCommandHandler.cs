using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Dialysis.EHR.PatientChart.Projections;

namespace Dialysis.EHR.PatientChart.Features.RecordVitalSign;

public sealed class RecordVitalSignCommandHandler : ICommandHandler<RecordVitalSignCommand, Guid>
{
    private readonly IVitalSignRepository _vitals;
    private readonly IUnitOfWork _unitOfWork;
    public RecordVitalSignCommandHandler(IVitalSignRepository vitals,
        IUnitOfWork unitOfWork)
    {
        _vitals = vitals;
        _unitOfWork = unitOfWork;
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

        // The aggregate raises the openEHR projection event; the SaveChanges interceptor drains it
        // into the Transponder outbox atomically with the reading — no manual bus publishing here.
        if (VitalSignOpenEhrProjector.Project(reading) is { } projection)
        {
            reading.RecordOpenEhrProjection(projection.ArchetypeId, projection.CompositionJson);
        }

        _vitals.Add(reading);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return id;
    }
}
