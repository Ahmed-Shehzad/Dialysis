using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.GetSessionSummary;

public sealed record SessionPrescriptionDto(
    string DialyzerModel,
    int PrescribedDurationMinutes,
    int BloodFlowRateMlPerMin,
    int DialysateFlowRateMlPerMin,
    decimal DialysatePotassiumMmolPerL,
    decimal DialysateCalciumMmolPerL,
    decimal DialysateSodiumMmolPerL,
    decimal TargetUfVolumeLiters,
    string AnticoagulationProtocolCode);

public sealed record VascularAccessDto(
    string Kind,
    string Site,
    DateOnly EstablishedOn);

public sealed record ReadingStatsDto(
    int Count,
    int? SystolicMin,
    int? SystolicMax,
    int? SystolicAvg,
    int? DiastolicMin,
    int? DiastolicMax,
    int? DiastolicAvg,
    int? HeartRateMin,
    int? HeartRateMax,
    int? HeartRateAvg,
    decimal? LastUltrafiltrationRateMlPerHour,
    DateTime? FirstObservedAtUtc,
    DateTime? LastObservedAtUtc);

public sealed record SessionSummaryDto(
    Guid Id,
    Guid PatientId,
    string Status,
    DateTime ScheduledStartUtc,
    DateTime? ActualStartUtc,
    DateTime? ActualEndUtc,
    int? ActualDurationMinutes,
    decimal? AchievedUfVolumeLiters,
    decimal? UfAchievementPercent,
    string? AbortReasonCode,
    Guid? MachineId,
    SessionPrescriptionDto Prescription,
    VascularAccessDto Access,
    ReadingStatsDto Readings);

public sealed record GetSessionSummaryQuery(Guid SessionId)
    : IQuery<SessionSummaryDto>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionRead;
}
