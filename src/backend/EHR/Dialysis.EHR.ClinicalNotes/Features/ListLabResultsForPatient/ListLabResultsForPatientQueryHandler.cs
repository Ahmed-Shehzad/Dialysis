using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;

public sealed class ListLabResultsForPatientQueryHandler : IQueryHandler<ListLabResultsForPatientQuery, IReadOnlyList<LabResultListItem>>
{
    private readonly ILabResultRepository _results;
    private readonly TimeProvider _time;
    public ListLabResultsForPatientQueryHandler(ILabResultRepository results,
        TimeProvider time)
    {
        _results = results;
        _time = time;
    }
    public async Task<IReadOnlyList<LabResultListItem>> HandleAsync(
        ListLabResultsForPatientQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var lookbackDays = Math.Clamp(request.LookbackDays, 1, 730);
        var sinceUtc = _time.GetUtcNow().UtcDateTime.AddDays(-lookbackDays);

        var rows = await _results
            .ListByPatientAsync(request.PatientId, sinceUtc, cancellationToken)
            .ConfigureAwait(false);

        return [.. rows
            .OrderByDescending(r => r.ObservedAtUtc)
            .Take(take)
            .Select(r => new LabResultListItem(
                r.Id,
                r.LabOrderId,
                r.LoincCode,
                r.ValueText,
                r.UnitCode,
                r.ReferenceRangeText,
                (int)r.AbnormalFlag,
                r.ObservedAtUtc))];
    }
}
