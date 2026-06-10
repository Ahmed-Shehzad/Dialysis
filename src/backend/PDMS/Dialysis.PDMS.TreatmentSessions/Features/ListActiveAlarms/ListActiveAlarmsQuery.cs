using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;

/// <summary>
/// Wire shape for one machine alarm. State strings are lower-cased so the SPA's
/// existing union (<c>"present" | "inactivating" | "resolved"</c>) is the source of truth.
/// </summary>
public sealed record ActiveAlarmDto
{
    /// <summary>
    /// Wire shape for one machine alarm. State strings are lower-cased so the SPA's
    /// existing union (<c>"present" | "inactivating" | "resolved"</c>) is the source of truth.
    /// </summary>
    public ActiveAlarmDto(Guid Id,
        Guid? SessionId,
        Guid MachineId,
        long AlarmCode,
        string? AlarmSource,
        string? AlarmPhase,
        string State,
        DateTime FirstObservedUtc,
        DateTime LastObservedUtc,
        DateTime? AcknowledgedUtc,
        string? AcknowledgedBy)
    {
        this.Id = Id;
        this.SessionId = SessionId;
        this.MachineId = MachineId;
        this.AlarmCode = AlarmCode;
        this.AlarmSource = AlarmSource;
        this.AlarmPhase = AlarmPhase;
        this.State = State;
        this.FirstObservedUtc = FirstObservedUtc;
        this.LastObservedUtc = LastObservedUtc;
        this.AcknowledgedUtc = AcknowledgedUtc;
        this.AcknowledgedBy = AcknowledgedBy;
    }
    public Guid Id { get; init; }
    public Guid? SessionId { get; init; }
    public Guid MachineId { get; init; }
    public long AlarmCode { get; init; }
    public string? AlarmSource { get; init; }
    public string? AlarmPhase { get; init; }
    public string State { get; init; }
    public DateTime FirstObservedUtc { get; init; }
    public DateTime LastObservedUtc { get; init; }
    public DateTime? AcknowledgedUtc { get; init; }
    public string? AcknowledgedBy { get; init; }
    public void Deconstruct(out Guid Id, out Guid? SessionId, out Guid MachineId, out long AlarmCode, out string? AlarmSource, out string? AlarmPhase, out string State, out DateTime FirstObservedUtc, out DateTime LastObservedUtc, out DateTime? AcknowledgedUtc, out string? AcknowledgedBy)
    {
        Id = this.Id;
        SessionId = this.SessionId;
        MachineId = this.MachineId;
        AlarmCode = this.AlarmCode;
        AlarmSource = this.AlarmSource;
        AlarmPhase = this.AlarmPhase;
        State = this.State;
        FirstObservedUtc = this.FirstObservedUtc;
        LastObservedUtc = this.LastObservedUtc;
        AcknowledgedUtc = this.AcknowledgedUtc;
        AcknowledgedBy = this.AcknowledgedBy;
    }
}

public sealed record ListActiveAlarmsQuery : IQuery<IReadOnlyList<ActiveAlarmDto>>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.AlarmRead;
}
