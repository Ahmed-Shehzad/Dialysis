using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.PDMS.Medications.Contracts;

namespace Dialysis.PDMS.OnCall.Domain;

/// <summary>
/// Defines how long the dispatcher waits at each chain link before walking to the next, per
/// alarm severity. A single policy can be reused across many rotations — the policy is global
/// to the facility unless explicitly overridden.
///
/// Default tunings reflect the chairside reality: critical alarms can't sit unacknowledged for
/// more than a minute; warnings get longer windows so the primary nurse isn't paged away from
/// patient bedside care for non-urgent events.
/// </summary>
public sealed class EscalationPolicy : AggregateRoot<Guid>
{
    private EscalationPolicy() { }

    public EscalationPolicy(
        Guid id,
        string name,
        TimeSpan criticalPrimaryWindow,
        TimeSpan criticalBackupWindow,
        TimeSpan warningPrimaryWindow,
        TimeSpan warningBackupWindow,
        TimeSpan informationalPrimaryWindow,
        bool quietHoursSuppressNonCritical) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EnsurePositive(criticalPrimaryWindow);
        EnsurePositive(criticalBackupWindow);
        EnsurePositive(warningPrimaryWindow);
        EnsurePositive(warningBackupWindow);
        EnsurePositive(informationalPrimaryWindow);
        Name = name;
        CriticalPrimaryWindow = criticalPrimaryWindow;
        CriticalBackupWindow = criticalBackupWindow;
        WarningPrimaryWindow = warningPrimaryWindow;
        WarningBackupWindow = warningBackupWindow;
        InformationalPrimaryWindow = informationalPrimaryWindow;
        QuietHoursSuppressNonCritical = quietHoursSuppressNonCritical;
    }

    public string Name { get; private set; } = null!;
    public TimeSpan CriticalPrimaryWindow { get; private set; }
    public TimeSpan CriticalBackupWindow { get; private set; }
    public TimeSpan WarningPrimaryWindow { get; private set; }
    public TimeSpan WarningBackupWindow { get; private set; }
    public TimeSpan InformationalPrimaryWindow { get; private set; }
    public bool QuietHoursSuppressNonCritical { get; private set; }

    /// <summary>
    /// Returns the delay before walking from <paramref name="currentAttemptIndex"/> to the next link.
    /// Returns <c>null</c> when the chain has been exhausted.
    /// </summary>
    public TimeSpan? DelayBeforeNextLink(IvPumpAlarmSeverity severity, int currentAttemptIndex)
        => (severity, currentAttemptIndex) switch
        {
            (IvPumpAlarmSeverity.Critical, 0) => CriticalPrimaryWindow,
            (IvPumpAlarmSeverity.Critical, 1) => CriticalBackupWindow,
            (IvPumpAlarmSeverity.Warning, 0) => WarningPrimaryWindow,
            (IvPumpAlarmSeverity.Warning, 1) => WarningBackupWindow,
            (IvPumpAlarmSeverity.Informational, 0) => InformationalPrimaryWindow,
            _ => null,
        };

    /// <summary>
    /// Platform default. Critical: 60s primary → 120s backup → supervisor. Warning: 5m → 10m.
    /// Informational: 15m primary only. Quiet-hours suppress non-critical pages.
    /// </summary>
    public static EscalationPolicy CreateDefault(Guid id) => new(
        id,
        "default",
        criticalPrimaryWindow: TimeSpan.FromSeconds(60),
        criticalBackupWindow: TimeSpan.FromSeconds(120),
        warningPrimaryWindow: TimeSpan.FromMinutes(5),
        warningBackupWindow: TimeSpan.FromMinutes(10),
        informationalPrimaryWindow: TimeSpan.FromMinutes(15),
        quietHoursSuppressNonCritical: true);

    private static void EnsurePositive(TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), "Escalation window must be positive.");
    }
}
