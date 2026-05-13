using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;

public sealed class CompleteSessionCommandHandler(
    IDialysisSessionRepository sessions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CompleteSessionCommand, Unit>
{
    public async Task<Unit> Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        session.Complete(timeProvider.GetUtcNow().UtcDateTime, request.AchievedUfVolumeLiters);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
