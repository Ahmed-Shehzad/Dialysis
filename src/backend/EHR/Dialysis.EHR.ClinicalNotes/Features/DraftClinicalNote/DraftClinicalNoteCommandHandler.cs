using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;

public sealed class DraftClinicalNoteCommandHandler(
    IClinicalNoteRepository notes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DraftClinicalNoteCommand, Guid>
{
    public async Task<Guid> HandleAsync(DraftClinicalNoteCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var note = ClinicalNote.Draft(
            id,
            request.EncounterId,
            request.PatientId,
            request.AuthoringProviderId,
            request.Subjective,
            request.Objective,
            request.Assessment,
            request.Plan);
        notes.Add(note);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
