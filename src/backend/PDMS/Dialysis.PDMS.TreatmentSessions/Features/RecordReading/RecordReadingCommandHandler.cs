using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Realtime;

namespace Dialysis.PDMS.TreatmentSessions.Features.RecordReading;

public sealed class RecordReadingCommandHandler : ICommandHandler<RecordReadingCommand, Guid>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IVitalsBroadcaster _broadcaster;
    public RecordReadingCommandHandler(IDialysisSessionRepository sessions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IVitalsBroadcaster broadcaster)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _broadcaster = broadcaster;
    }
    public async Task<Guid> HandleAsync(RecordReadingCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{request.SessionId}' not found.");

        var reading = session.RecordReading(
            _timeProvider.GetUtcNow().UtcDateTime,
            request.SystolicBloodPressure,
            request.DiastolicBloodPressure,
            request.HeartRateBpm,
            request.ArterialPressureMmHg,
            request.VenousPressureMmHg,
            request.UltrafiltrationRateMlPerHour,
            request.ConductivityMsPerCm,
            request.Notes,
            explicitReadingId: request.ReadingId == Guid.Empty ? null : request.ReadingId);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _broadcaster.BroadcastAsync(
            new VitalsReadingSnapshot(
                reading.Id,
                reading.SessionId,
                reading.ObservedAtUtc,
                reading.SystolicBloodPressure,
                reading.DiastolicBloodPressure,
                reading.HeartRateBpm,
                reading.ArterialPressureMmHg,
                reading.VenousPressureMmHg,
                reading.UltrafiltrationRateMlPerHour,
                reading.ConductivityMsPerCm,
                reading.Notes),
            cancellationToken).ConfigureAwait(false);

        return reading.Id;
    }
}
