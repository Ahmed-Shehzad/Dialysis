using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;

public sealed class ListLabResultsForPatientQueryHandler(
    ILabResultRepository results,
    TimeProvider time)
    : IQueryHandler<ListLabResultsForPatientQuery, IReadOnlyList<LabResultListItem>>
{
    public async Task<IReadOnlyList<LabResultListItem>> HandleAsync(
        ListLabResultsForPatientQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var lookbackDays = Math.Clamp(request.LookbackDays, 1, 730);
        var sinceUtc = time.GetUtcNow().UtcDateTime.AddDays(-lookbackDays);

        var rows = await results
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
