using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.GetSessionSummary;

public sealed record SessionPrescriptionDto
{
    public SessionPrescriptionDto(string DialyzerModel,
        int PrescribedDurationMinutes,
        int BloodFlowRateMlPerMin,
        int DialysateFlowRateMlPerMin,
        decimal DialysatePotassiumMmolPerL,
        decimal DialysateCalciumMmolPerL,
        decimal DialysateSodiumMmolPerL,
        decimal TargetUfVolumeLiters,
        string AnticoagulationProtocolCode)
    {
        this.DialyzerModel = DialyzerModel;
        this.PrescribedDurationMinutes = PrescribedDurationMinutes;
        this.BloodFlowRateMlPerMin = BloodFlowRateMlPerMin;
        this.DialysateFlowRateMlPerMin = DialysateFlowRateMlPerMin;
        this.DialysatePotassiumMmolPerL = DialysatePotassiumMmolPerL;
        this.DialysateCalciumMmolPerL = DialysateCalciumMmolPerL;
        this.DialysateSodiumMmolPerL = DialysateSodiumMmolPerL;
        this.TargetUfVolumeLiters = TargetUfVolumeLiters;
        this.AnticoagulationProtocolCode = AnticoagulationProtocolCode;
    }
    public string DialyzerModel { get; init; }
    public int PrescribedDurationMinutes { get; init; }
    public int BloodFlowRateMlPerMin { get; init; }
    public int DialysateFlowRateMlPerMin { get; init; }
    public decimal DialysatePotassiumMmolPerL { get; init; }
    public decimal DialysateCalciumMmolPerL { get; init; }
    public decimal DialysateSodiumMmolPerL { get; init; }
    public decimal TargetUfVolumeLiters { get; init; }
    public string AnticoagulationProtocolCode { get; init; }
    public void Deconstruct(out string dialyzerModel, out int prescribedDurationMinutes, out int bloodFlowRateMlPerMin, out int dialysateFlowRateMlPerMin, out decimal dialysatePotassiumMmolPerL, out decimal dialysateCalciumMmolPerL, out decimal dialysateSodiumMmolPerL, out decimal targetUfVolumeLiters, out string anticoagulationProtocolCode)
    {
        dialyzerModel = DialyzerModel;
        prescribedDurationMinutes = PrescribedDurationMinutes;
        bloodFlowRateMlPerMin = BloodFlowRateMlPerMin;
        dialysateFlowRateMlPerMin = DialysateFlowRateMlPerMin;
        dialysatePotassiumMmolPerL = DialysatePotassiumMmolPerL;
        dialysateCalciumMmolPerL = DialysateCalciumMmolPerL;
        dialysateSodiumMmolPerL = DialysateSodiumMmolPerL;
        targetUfVolumeLiters = TargetUfVolumeLiters;
        anticoagulationProtocolCode = AnticoagulationProtocolCode;
    }
}

public sealed record VascularAccessDto
{
    public VascularAccessDto(string Kind,
        string Site,
        DateOnly EstablishedOn)
    {
        this.Kind = Kind;
        this.Site = Site;
        this.EstablishedOn = EstablishedOn;
    }
    public string Kind { get; init; }
    public string Site { get; init; }
    public DateOnly EstablishedOn { get; init; }
    public void Deconstruct(out string kind, out string site, out DateOnly establishedOn)
    {
        kind = Kind;
        site = Site;
        establishedOn = EstablishedOn;
    }
}

public sealed record ReadingStatsDto
{
    public ReadingStatsDto(int Count,
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
        DateTime? LastObservedAtUtc)
    {
        this.Count = Count;
        this.SystolicMin = SystolicMin;
        this.SystolicMax = SystolicMax;
        this.SystolicAvg = SystolicAvg;
        this.DiastolicMin = DiastolicMin;
        this.DiastolicMax = DiastolicMax;
        this.DiastolicAvg = DiastolicAvg;
        this.HeartRateMin = HeartRateMin;
        this.HeartRateMax = HeartRateMax;
        this.HeartRateAvg = HeartRateAvg;
        this.LastUltrafiltrationRateMlPerHour = LastUltrafiltrationRateMlPerHour;
        this.FirstObservedAtUtc = FirstObservedAtUtc;
        this.LastObservedAtUtc = LastObservedAtUtc;
    }
    public int Count { get; init; }
    public int? SystolicMin { get; init; }
    public int? SystolicMax { get; init; }
    public int? SystolicAvg { get; init; }
    public int? DiastolicMin { get; init; }
    public int? DiastolicMax { get; init; }
    public int? DiastolicAvg { get; init; }
    public int? HeartRateMin { get; init; }
    public int? HeartRateMax { get; init; }
    public int? HeartRateAvg { get; init; }
    public decimal? LastUltrafiltrationRateMlPerHour { get; init; }
    public DateTime? FirstObservedAtUtc { get; init; }
    public DateTime? LastObservedAtUtc { get; init; }
    public void Deconstruct(out int count, out int? systolicMin, out int? systolicMax, out int? systolicAvg, out int? diastolicMin, out int? diastolicMax, out int? diastolicAvg, out int? heartRateMin, out int? heartRateMax, out int? heartRateAvg, out decimal? lastUltrafiltrationRateMlPerHour, out DateTime? firstObservedAtUtc, out DateTime? lastObservedAtUtc)
    {
        count = Count;
        systolicMin = SystolicMin;
        systolicMax = SystolicMax;
        systolicAvg = SystolicAvg;
        diastolicMin = DiastolicMin;
        diastolicMax = DiastolicMax;
        diastolicAvg = DiastolicAvg;
        heartRateMin = HeartRateMin;
        heartRateMax = HeartRateMax;
        heartRateAvg = HeartRateAvg;
        lastUltrafiltrationRateMlPerHour = LastUltrafiltrationRateMlPerHour;
        firstObservedAtUtc = FirstObservedAtUtc;
        lastObservedAtUtc = LastObservedAtUtc;
    }
}

public sealed record SessionSummaryDto
{
    public SessionSummaryDto(Guid Id,
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
        DateTime? PausedAtUtc,
        int AccumulatedPausedSeconds,
        SessionPrescriptionDto Prescription,
        VascularAccessDto Access,
        ReadingStatsDto Readings)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.Status = Status;
        this.ScheduledStartUtc = ScheduledStartUtc;
        this.ActualStartUtc = ActualStartUtc;
        this.ActualEndUtc = ActualEndUtc;
        this.ActualDurationMinutes = ActualDurationMinutes;
        this.AchievedUfVolumeLiters = AchievedUfVolumeLiters;
        this.UfAchievementPercent = UfAchievementPercent;
        this.AbortReasonCode = AbortReasonCode;
        this.MachineId = MachineId;
        this.PausedAtUtc = PausedAtUtc;
        this.AccumulatedPausedSeconds = AccumulatedPausedSeconds;
        this.Prescription = Prescription;
        this.Access = Access;
        this.Readings = Readings;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string Status { get; init; }
    public DateTime ScheduledStartUtc { get; init; }
    public DateTime? ActualStartUtc { get; init; }
    public DateTime? ActualEndUtc { get; init; }
    /// <summary>Machine usage time on completion (wall-clock minus paused spans); null until the session ends.</summary>
    public int? ActualDurationMinutes { get; init; }
    public decimal? AchievedUfVolumeLiters { get; init; }
    public decimal? UfAchievementPercent { get; init; }
    public string? AbortReasonCode { get; init; }
    public Guid? MachineId { get; init; }
    /// <summary>When the session entered its current pause, or null while running / ended.</summary>
    public DateTime? PausedAtUtc { get; init; }
    /// <summary>Total seconds spent paused so far, excluding any open pause.</summary>
    public int AccumulatedPausedSeconds { get; init; }
    public SessionPrescriptionDto Prescription { get; init; }
    public VascularAccessDto Access { get; init; }
    public ReadingStatsDto Readings { get; init; }
    public void Deconstruct(out Guid id, out Guid patientId, out string status, out DateTime scheduledStartUtc, out DateTime? actualStartUtc, out DateTime? actualEndUtc, out int? actualDurationMinutes, out decimal? achievedUfVolumeLiters, out decimal? ufAchievementPercent, out string? abortReasonCode, out Guid? machineId, out DateTime? pausedAtUtc, out int accumulatedPausedSeconds, out SessionPrescriptionDto prescription, out VascularAccessDto access, out ReadingStatsDto readings)
    {
        id = Id;
        patientId = PatientId;
        status = Status;
        scheduledStartUtc = ScheduledStartUtc;
        actualStartUtc = ActualStartUtc;
        actualEndUtc = ActualEndUtc;
        actualDurationMinutes = ActualDurationMinutes;
        achievedUfVolumeLiters = AchievedUfVolumeLiters;
        ufAchievementPercent = UfAchievementPercent;
        abortReasonCode = AbortReasonCode;
        machineId = MachineId;
        pausedAtUtc = PausedAtUtc;
        accumulatedPausedSeconds = AccumulatedPausedSeconds;
        prescription = Prescription;
        access = Access;
        readings = Readings;
    }
}

public sealed record GetSessionSummaryQuery : IQuery<SessionSummaryDto>, IPermissionedCommand
{
    public GetSessionSummaryQuery(Guid SessionId) => this.SessionId = SessionId;
    public string RequiredPermission => PdmsPermissions.SessionRead;
    public Guid SessionId { get; init; }
    public void Deconstruct(out Guid sessionId) => sessionId = SessionId;
}
