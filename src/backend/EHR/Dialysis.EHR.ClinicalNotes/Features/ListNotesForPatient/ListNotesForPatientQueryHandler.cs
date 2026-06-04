using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;

public sealed class ListNotesForPatientQueryHandler : IQueryHandler<ListNotesForPatientQuery, IReadOnlyList<ClinicalNoteListItem>>
{
    private readonly IClinicalNoteRepository _notes;
    public ListNotesForPatientQueryHandler(IClinicalNoteRepository notes) => _notes = notes;
    public async Task<IReadOnlyList<ClinicalNoteListItem>> HandleAsync(
        ListNotesForPatientQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 200);
        var rows = await _notes.ListByPatientAsync(request.PatientId, take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(n => new ClinicalNoteListItem(
            n.Id,
            n.EncounterId,
            n.AuthoringProviderId,
            (int)n.Status,
            n.CreatedAt,
            n.SignedAtUtc,
            n.Subjective,
            n.Objective,
            n.Assessment,
            n.Plan))];
    }
}
