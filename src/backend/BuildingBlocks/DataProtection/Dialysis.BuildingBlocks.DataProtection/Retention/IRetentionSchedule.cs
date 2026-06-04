namespace Dialysis.BuildingBlocks.DataProtection.Retention;

/// <summary>
/// Per-aggregate retention windows. GDPR Art. 5(1)(e) requires data be kept "no longer than
/// necessary"; German law extends specific minimums (Berufsordnung §10 → 30 years for
/// clinical records; HGB §257 → 10 years for billing; AO §147 → 6 years for ancillary docs).
/// Each module registers its retention keys at startup; the platform-wide
/// `RetentionPrunerHostedService` enforces them.
/// </summary>
public interface IRetentionSchedule
{
    /// <summary>Resolves the retention window for a given key. <c>null</c> = unknown key
    /// (the pruner skips rather than risking a too-aggressive delete).</summary>
    RetentionWindow? Resolve(string retentionKey);

    /// <summary>All registered windows; used by the RoPA generator + DPIA template.</summary>
    IReadOnlyList<RetentionWindowRegistration> All();
}

/// <summary>One retention window: minimum + maximum + the legal authority justifying it.</summary>
public readonly record struct RetentionWindow
{
    /// <summary>One retention window: minimum + maximum + the legal authority justifying it.</summary>
    public RetentionWindow(TimeSpan Minimum,
        TimeSpan Maximum,
        string LegalAuthority)
    {
        this.Minimum = Minimum;
        this.Maximum = Maximum;
        this.LegalAuthority = LegalAuthority;
    }
    public TimeSpan Minimum { get; init; }
    public TimeSpan Maximum { get; init; }
    public string LegalAuthority { get; init; }
    public void Deconstruct(out TimeSpan Minimum, out TimeSpan Maximum, out string LegalAuthority)
    {
        Minimum = this.Minimum;
        Maximum = this.Maximum;
        LegalAuthority = this.LegalAuthority;
    }
}

public sealed record RetentionWindowRegistration
{
    public RetentionWindowRegistration(string Key,
        RetentionWindow Window,
        string Description)
    {
        this.Key = Key;
        this.Window = Window;
        this.Description = Description;
    }
    public string Key { get; init; }
    public RetentionWindow Window { get; init; }
    public string Description { get; init; }
    public void Deconstruct(out string Key, out RetentionWindow Window, out string Description)
    {
        Key = this.Key;
        Window = this.Window;
        Description = this.Description;
    }
}
