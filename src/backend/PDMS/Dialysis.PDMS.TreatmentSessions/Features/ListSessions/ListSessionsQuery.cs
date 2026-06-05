using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessions;

public sealed record DialysisSessionListItem
{
    public DialysisSessionListItem(Guid Id,
        Guid PatientId,
        string Status,
        DateTime ScheduledStartUtc,
        DateTime? ActualStartUtc,
        DateTime? ActualEndUtc,
        Guid? MachineId,
        DateTime? PausedAtUtc,
        int AccumulatedPausedSeconds)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.Status = Status;
        this.ScheduledStartUtc = ScheduledStartUtc;
        this.ActualStartUtc = ActualStartUtc;
        this.ActualEndUtc = ActualEndUtc;
        this.MachineId = MachineId;
        this.PausedAtUtc = PausedAtUtc;
        this.AccumulatedPausedSeconds = AccumulatedPausedSeconds;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string Status { get; init; }
    public DateTime ScheduledStartUtc { get; init; }
    public DateTime? ActualStartUtc { get; init; }
    public DateTime? ActualEndUtc { get; init; }
    public Guid? MachineId { get; init; }
    /// <summary>When the session entered its current pause, or null while running / ended.</summary>
    public DateTime? PausedAtUtc { get; init; }
    /// <summary>Total seconds spent paused so far (excluding any open pause) — lets the live timer exclude pauses.</summary>
    public int AccumulatedPausedSeconds { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string Status, out DateTime ScheduledStartUtc, out DateTime? ActualStartUtc, out DateTime? ActualEndUtc, out Guid? MachineId, out DateTime? PausedAtUtc, out int AccumulatedPausedSeconds)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        Status = this.Status;
        ScheduledStartUtc = this.ScheduledStartUtc;
        ActualStartUtc = this.ActualStartUtc;
        ActualEndUtc = this.ActualEndUtc;
        MachineId = this.MachineId;
        PausedAtUtc = this.PausedAtUtc;
        AccumulatedPausedSeconds = this.AccumulatedPausedSeconds;
    }
}

public sealed record ListSessionsQuery : IQuery<IReadOnlyList<DialysisSessionListItem>>, IPermissionedCommand
{
    public ListSessionsQuery(bool ActiveOnly = false, int Take = 50)
    {
        this.ActiveOnly = ActiveOnly;
        this.Take = Take;
    }
    public string RequiredPermission => PdmsPermissions.SessionRead;
    public bool ActiveOnly { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out bool ActiveOnly, out int Take)
    {
        ActiveOnly = this.ActiveOnly;
        Take = this.Take;
    }
}
