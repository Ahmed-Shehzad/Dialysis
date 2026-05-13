using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;

public sealed class SignClinicalNoteCommandHandler(
    IClinicalNoteRepository notes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SignClinicalNoteCommand, Unit>
{
    public async Task<Unit> Handle(SignClinicalNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await notes.GetAsync(request.NoteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Clinical note '{request.NoteId}' not found.");
        note.Sign(request.SigningProviderId, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
