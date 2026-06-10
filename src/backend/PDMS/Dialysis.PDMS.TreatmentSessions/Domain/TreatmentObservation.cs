using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

/// <summary>
/// A single dialysis-machine observation persisted from an ORU^R01 OBX segment. Mirrors the wire shape:
/// an ISO/IEEE 11073 MDC numeric code, the containment-tree path inside the machine's MDS → VMD → Channel
/// → Metric hierarchy, plus exactly one of <see cref="ValueNumeric"/> / <see cref="ValueString"/> / a profile
/// array pair. <see cref="SourceMessageId"/> ties the row back to the original
/// <c>raw_hl7_messages</c> row for audit and reparse.
/// </summary>
public sealed class TreatmentObservation : Entity<Guid>
{
    private TreatmentObservation()
    {
    }

    public TreatmentObservation(Guid id) : base(id)
    {
    }

    public Guid SessionId { get; private set; }

    public Guid MachineId { get; private set; }

    public DateTime ObservedAtUtc { get; private set; }

    public long MdcCode { get; private set; }

    public string ContainmentPath { get; private set; } = null!;

    public decimal? ValueNumeric { get; private set; }

    public string? ValueString { get; private set; }

    public string? Units { get; private set; }

    public decimal[]? ProfileValues { get; private set; }

    public int[]? ProfileTimesSeconds { get; private set; }

    public Guid SourceMessageId { get; private set; }

    public static TreatmentObservation Record(
        Guid id,
        Guid sessionId,
        Guid machineId,
        DateTime observedAtUtc,
        long mdcCode,
        string containmentPath,
        decimal? valueNumeric,
        string? valueString,
        string? units,
        decimal[]? profileValues,
        int[]? profileTimesSeconds,
        Guid sourceMessageId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session required.", nameof(sessionId));
        if (machineId == Guid.Empty)
            throw new ArgumentException("Machine required.", nameof(machineId));
        if (mdcCode <= 0)
            throw new ArgumentOutOfRangeException(nameof(mdcCode), "MDC code must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(containmentPath);
        if (containmentPath.Length > 64)
            throw new ArgumentException("Containment path too long.", nameof(containmentPath));

        var hasNumeric = valueNumeric.HasValue;
        var hasString = !string.IsNullOrEmpty(valueString);
        var hasProfile = profileValues is { Length: > 0 };
        var present = (hasNumeric ? 1 : 0) + (hasString ? 1 : 0) + (hasProfile ? 1 : 0);
        if (present == 0)
            throw new ArgumentException("Observation must carry one of numeric, string, or profile value.", nameof(valueNumeric));
        if (present > 1)
            throw new ArgumentException("Observation may carry only one of numeric, string, or profile value.", nameof(valueNumeric));

        if (hasProfile && profileTimesSeconds is { Length: > 0 } times && times.Length != profileValues!.Length)
            throw new ArgumentException("Profile time array must match profile value length when both are present.", nameof(profileTimesSeconds));

        return new TreatmentObservation(id)
        {
            SessionId = sessionId,
            MachineId = machineId,
            ObservedAtUtc = observedAtUtc,
            MdcCode = mdcCode,
            ContainmentPath = containmentPath.Trim(),
            ValueNumeric = valueNumeric,
            ValueString = hasString ? valueString!.Trim() : null,
            Units = string.IsNullOrWhiteSpace(units) ? null : units.Trim(),
            ProfileValues = hasProfile ? profileValues : null,
            ProfileTimesSeconds = hasProfile ? profileTimesSeconds : null,
            SourceMessageId = sourceMessageId,
        };
    }
}
