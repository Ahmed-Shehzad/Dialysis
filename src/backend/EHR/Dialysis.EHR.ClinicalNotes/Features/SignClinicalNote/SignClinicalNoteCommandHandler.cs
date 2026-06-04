using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;

public sealed class SignClinicalNoteCommandHandler : ICommandHandler<SignClinicalNoteCommand, Unit>
{
    private readonly IClinicalNoteRepository _notes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public SignClinicalNoteCommandHandler(IClinicalNoteRepository notes,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _notes = notes;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(SignClinicalNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _notes.GetAsync(request.NoteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Clinical note '{request.NoteId}' not found.");
        note.Sign(request.SigningProviderId, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
