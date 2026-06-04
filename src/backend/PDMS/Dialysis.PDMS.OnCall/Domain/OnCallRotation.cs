using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.OnCall.Domain;

/// <summary>
/// Active clinician rotation for one chair within one shift window. The rotation captures who
/// gets paged first when an IV-pump alarm fires on the chair, plus the backup and supervisor
/// fallback so the escalation policy has somewhere to walk to on no-response.
///
/// Rotations are operator-maintained (drag-and-drop nurse assignment in the admin UI). One
/// chair has at most one active rotation per shift; older rotations stay in the table as
/// historical audit trail (the platform-wide retention policy covers them).
/// </summary>
public sealed class OnCallRotation : AggregateRoot<Guid>
{
    private OnCallRotation() { }

    public OnCallRotation(
        Guid id,
        Guid chairId,
        OnCallShift shift,
        DateOnly effectiveFromUtc,
        DateOnly effectiveUntilUtc,
        OnCallChainLink primary,
        OnCallChainLink backup,
        OnCallChainLink supervisor) : base(id)
    {
        if (effectiveUntilUtc < effectiveFromUtc)
            throw new ArgumentException("Rotation effective-until must be ≥ effective-from.", nameof(effectiveUntilUtc));
        ChairId = chairId;
        Shift = shift;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveUntilUtc = effectiveUntilUtc;
        Primary = primary;
        Backup = backup;
        Supervisor = supervisor;
    }

    public Guid ChairId { get; private set; }
    public OnCallShift Shift { get; private set; } = null!;
    public DateOnly EffectiveFromUtc { get; private set; }
    public DateOnly EffectiveUntilUtc { get; private set; }
    public OnCallChainLink Primary { get; private set; } = null!;
    public OnCallChainLink Backup { get; private set; } = null!;
    public OnCallChainLink Supervisor { get; private set; } = null!;

    /// <summary>True when the rotation covers <paramref name="atUtc"/>.</summary>
    public bool CoversInstant(DateTime atUtc)
    {
        var date = DateOnly.FromDateTime(atUtc);
        return date >= EffectiveFromUtc && date <= EffectiveUntilUtc && Shift.Covers(atUtc);
    }

    /// <summary>Returns the chain link for a given attempt index (0=primary, 1=backup, 2=supervisor).</summary>
    public OnCallChainLink? LinkForAttempt(int attemptIndex) => attemptIndex switch
    {
        0 => Primary,
        1 => Backup,
        2 => Supervisor,
        _ => null,
    };
}

/// <summary>Clinical shift the rotation covers. Times are local to the facility's timezone.</summary>
public sealed record OnCallShift
{
    /// <summary>Clinical shift the rotation covers. Times are local to the facility's timezone.</summary>
    public OnCallShift(string Code, TimeOnly StartLocal, TimeOnly EndLocal)
    {
        this.Code = Code;
        this.StartLocal = StartLocal;
        this.EndLocal = EndLocal;
    }

    /// <summary>Morning, 06:00–14:00.</summary>
    public static OnCallShift Morning => new("morning", new TimeOnly(6, 0), new TimeOnly(14, 0));

    /// <summary>Afternoon, 14:00–22:00.</summary>
    public static OnCallShift Afternoon => new("afternoon", new TimeOnly(14, 0), new TimeOnly(22, 0));

    /// <summary>Night, 22:00–06:00 (wraps midnight).</summary>
    public static OnCallShift Night => new("night", new TimeOnly(22, 0), new TimeOnly(6, 0));

    public string Code { get; init; }
    public TimeOnly StartLocal { get; init; }
    public TimeOnly EndLocal { get; init; }

    public bool Covers(DateTime atUtc)
    {
        var t = TimeOnly.FromDateTime(atUtc);
        if (StartLocal <= EndLocal)
            return t >= StartLocal && t < EndLocal;
        return t >= StartLocal || t < EndLocal;
    }
    public void Deconstruct(out string Code, out TimeOnly StartLocal, out TimeOnly EndLocal)
    {
        Code = this.Code;
        StartLocal = this.StartLocal;
        EndLocal = this.EndLocal;
    }
}

/// <summary>
/// One step in the escalation chain — the clinician identifier, their preferred channels,
/// and contact handles. Channels are tried in order until acknowledged or the escalation
/// policy's delay elapses.
/// </summary>
public sealed record OnCallChainLink
{
    /// <summary>
    /// One step in the escalation chain — the clinician identifier, their preferred channels,
    /// and contact handles. Channels are tried in order until acknowledged or the escalation
    /// policy's delay elapses.
    /// </summary>
    public OnCallChainLink(string ClinicianSub,
        string DisplayName,
        IReadOnlyList<NotificationChannelTarget> Channels)
    {
        this.ClinicianSub = ClinicianSub;
        this.DisplayName = DisplayName;
        this.Channels = Channels;
    }
    public string ClinicianSub { get; init; }
    public string DisplayName { get; init; }
    public IReadOnlyList<NotificationChannelTarget> Channels { get; init; }
    public void Deconstruct(out string ClinicianSub, out string DisplayName, out IReadOnlyList<NotificationChannelTarget> Channels)
    {
        ClinicianSub = this.ClinicianSub;
        DisplayName = this.DisplayName;
        Channels = this.Channels;
    }
}

/// <summary>One channel handle for a clinician — e.g. an SMS phone number or an APNs device token.</summary>
public sealed record NotificationChannelTarget
{
    /// <summary>One channel handle for a clinician — e.g. an SMS phone number or an APNs device token.</summary>
    public NotificationChannelTarget(NotificationChannel Channel, string Address)
    {
        this.Channel = Channel;
        this.Address = Address;
    }
    public NotificationChannel Channel { get; init; }
    public string Address { get; init; }
    public void Deconstruct(out NotificationChannel Channel, out string Address)
    {
        Channel = this.Channel;
        Address = this.Address;
    }
}

public enum NotificationChannel
{
    Sms = 0,
    PushApns = 1,
    PushFcm = 2,
    Email = 3,
    Voice = 4,
}
