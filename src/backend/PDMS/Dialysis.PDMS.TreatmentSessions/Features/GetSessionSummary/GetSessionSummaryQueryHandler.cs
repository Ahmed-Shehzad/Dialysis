using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.GetSessionSummary;

public sealed class GetSessionSummaryQueryHandler(IDialysisSessionRepository sessions)
    : IQueryHandler<GetSessionSummaryQuery, SessionSummaryDto>
{
    public async Task<SessionSummaryDto> HandleAsync(
        GetSessionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Session '{request.SessionId}' not found.");

        var readings = session.Readings.OrderBy(r => r.ObservedAtUtc).ToList();

        var stats = readings.Count == 0
            ? new ReadingStatsDto(0, null, null, null, null, null, null, null, null, null, null, null, null)
            : new ReadingStatsDto(
                readings.Count,
                readings.Min(r => r.SystolicBloodPressure),
                readings.Max(r => r.SystolicBloodPressure),
                (int)Math.Round(readings.Average(r => r.SystolicBloodPressure)),
                readings.Min(r => r.DiastolicBloodPressure),
                readings.Max(r => r.DiastolicBloodPressure),
                (int)Math.Round(readings.Average(r => r.DiastolicBloodPressure)),
                readings.Min(r => r.HeartRateBpm),
                readings.Max(r => r.HeartRateBpm),
                (int)Math.Round(readings.Average(r => r.HeartRateBpm)),
                readings[^1].UltrafiltrationRateMlPerHour,
                readings[0].ObservedAtUtc,
                readings[^1].ObservedAtUtc);

        int? actualDurationMinutes = session.ActualStartUtc.HasValue && session.ActualEndUtc.HasValue
            ? (int)Math.Round((session.ActualEndUtc.Value - session.ActualStartUtc.Value).TotalMinutes)
            : null;

        decimal? ufAchievementPercent = session.AchievedUfVolumeLiters.HasValue
            && session.Prescription.TargetUfVolumeLiters > 0
            ? Math.Round(
                session.AchievedUfVolumeLiters.Value / session.Prescription.TargetUfVolumeLiters * 100m,
                1)
            : null;

        return new SessionSummaryDto(
            session.Id,
            session.PatientId,
            session.Status.ToString(),
            session.ScheduledStartUtc,
            session.ActualStartUtc,
            session.ActualEndUtc,
            actualDurationMinutes,
            session.AchievedUfVolumeLiters,
            ufAchievementPercent,
            session.AbortReasonCode,
            session.MachineId,
            new SessionPrescriptionDto(
                session.Prescription.DialyzerModel,
                session.Prescription.PrescribedDurationMinutes,
                session.Prescription.BloodFlowRateMlPerMin,
                session.Prescription.DialysateFlowRateMlPerMin,
                session.Prescription.DialysatePotassiumMmolPerL,
                session.Prescription.DialysateCalciumMmolPerL,
                session.Prescription.DialysateSodiumMmolPerL,
                session.Prescription.TargetUfVolumeLiters,
                session.Prescription.AnticoagulationProtocolCode),
            new VascularAccessDto(
                session.Access.Kind.ToString(),
                session.Access.Site,
                session.Access.EstablishedOn),
            stats);
    }
}
