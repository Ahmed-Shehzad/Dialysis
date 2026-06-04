using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;

public sealed class DraftClinicalNoteCommandHandler : ICommandHandler<DraftClinicalNoteCommand, Guid>
{
    private readonly IClinicalNoteRepository _notes;
    private readonly IUnitOfWork _unitOfWork;
    public DraftClinicalNoteCommandHandler(IClinicalNoteRepository notes,
        IUnitOfWork unitOfWork)
    {
        _notes = notes;
        _unitOfWork = unitOfWork;
    }
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
        _notes.Add(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
