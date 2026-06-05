using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.PauseSession;

/// <summary>Loads the session, pauses it at the current instant, and persists the change.</summary>
public sealed class PauseSessionCommandHandler : ICommandHandler<PauseSessionCommand, Unit>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public PauseSessionCommandHandler(
        IDialysisSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(PauseSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");
        session.Pause(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
