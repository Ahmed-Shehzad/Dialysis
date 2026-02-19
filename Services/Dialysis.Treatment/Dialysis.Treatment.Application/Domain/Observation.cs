using BuildingBlocks;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain;

/// <summary>
/// Parameters required to create an <see cref="Observation"/>.
/// </summary>
public sealed record ObservationCreateParams(
    ObservationCode Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? ReferenceRange,
    ObservationStatus? ResultStatus,
    DateTimeOffset? EffectiveTime,
    string? Provenance,
    string? EquipmentInstanceId,
    ContainmentLevel? Level,
    double? MessageTimeDriftSeconds = null);

public sealed class Observation : BaseEntity
{
    public Ulid TreatmentSessionId { get; private set; }
    public ObservationCode Code { get; private set; }
    public string? Value { get; private set; }
    public string? Unit { get; private set; }
    /// <summary>OBX-4 dotted sub-ID encoding IEEE 11073 containment path (e.g. "1.2.3.4").</summary>
    public string? SubId { get; private set; }
    /// <summary>OBX-7 reference range (e.g. "90-140").</summary>
    public string? ReferenceRange { get; private set; }
    /// <summary>OBX-11 observation result status (F, C, P, X).</summary>
    public ObservationStatus? ResultStatus { get; private set; }
    /// <summary>OBX-14 observation timestamp.</summary>
    public DateTimeOffset? EffectiveTime { get; private set; }
    /// <summary>OBX-17 provenance (observation method / equipment ID).</summary>
    public string? Provenance { get; private set; }
    /// <summary>OBX-18 equipment instance identifier.</summary>
    public string? EquipmentInstanceId { get; private set; }
    /// <summary>Containment level in the IEEE 11073 hierarchy.</summary>
    public ContainmentLevel? Level { get; private set; }
    public DateTimeOffset ObservedAtUtc { get; private set; }
    /// <summary>Absolute drift in seconds between MSH-7 and server UTC (IHE CT audit).</summary>
    public double? MessageTimeDriftSeconds { get; private set; }

    private Observation() { }

    internal static Observation Create(Ulid treatmentSessionId, ObservationCreateParams p)
    {
        return new Observation
        {
            TreatmentSessionId = treatmentSessionId,
            Code = p.Code,
            Value = p.Value,
            Unit = p.Unit,
            SubId = p.SubId,
            ReferenceRange = p.ReferenceRange,
            ResultStatus = p.ResultStatus,
            EffectiveTime = p.EffectiveTime,
            Provenance = p.Provenance,
            EquipmentInstanceId = p.EquipmentInstanceId,
            Level = p.Level,
            ObservedAtUtc = DateTimeOffset.UtcNow,
            MessageTimeDriftSeconds = p.MessageTimeDriftSeconds
        };
    }
}
