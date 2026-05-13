using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.AbortSession;

public sealed class AbortSessionCommandHandler(
    IDialysisSessionRepository sessions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AbortSessionCommand, Unit>
{
    public async Task<Unit> Handle(AbortSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        session.Abort(timeProvider.GetUtcNow().UtcDateTime, request.ReasonCode);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
