using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Integration.Projections;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

public sealed class IngestLabResultCommandHandler : ICommandHandler<IngestLabResultCommand, Guid>
{
    private readonly ILabResultRepository _labResults;
    private readonly IUnitOfWork _unitOfWork;
    public IngestLabResultCommandHandler(ILabResultRepository labResults,
        IUnitOfWork unitOfWork)
    {
        _labResults = labResults;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(IngestLabResultCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var resultId = Guid.CreateVersion7();

        // Persist the chart read-model row so GET /patients/{id}/lab-results surfaces it. Without this
        // the order would stay result-less on the chart (the read model was never written before).
        // The aggregate raises LabResultReceived + LabResultProjectedAsOpenEhr; the SaveChanges
        // interceptor drains both into the Transponder outbox atomically with the row — no manual
        // bus publishing here.
        var labResult = LabResult.Receive(
            resultId,
            request.LabOrderId,
            request.PatientId,
            request.LoincCode,
            request.ValueText,
            MapAbnormalFlag(request.AbnormalFlagCode),
            request.ObservedAtUtc,
            request.UnitCode,
            request.ReferenceRangeText,
            request.AbnormalFlagCode);

        var projection = LabResultOpenEhrProjector.Project(request, resultId);
        labResult.RecordOpenEhrProjection(projection.ArchetypeId, projection.CompositionJson);

        _labResults.Add(labResult);
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
