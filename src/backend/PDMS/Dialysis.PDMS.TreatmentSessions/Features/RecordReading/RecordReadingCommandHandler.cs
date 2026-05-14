using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordReading;

public sealed class RecordReadingCommandHandler(
    IDialysisSessionRepository sessions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RecordReadingCommand, Guid>
{
    public async Task<Guid> HandleAsync(RecordReadingCommand request, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");

        var reading = session.RecordReading(
            timeProvider.GetUtcNow().UtcDateTime,
            request.SystolicBloodPressure,
            request.DiastolicBloodPressure,
            request.HeartRateBpm,
            request.ArterialPressureMmHg,
            request.VenousPressureMmHg,
            request.UltrafiltrationRateMlPerHour,
            request.ConductivityMsPerCm,
            request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return reading.Id;
    }
}
