using BuildingBlocks;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain;

public sealed class Observation : BaseEntity
{
    public Ulid TreatmentSessionId { get; private set; }
    public ObservationCode Code { get; private set; }
    public string? Value { get; private set; }
    public string? Unit { get; private set; }
    public string? SubId { get; private set; }
    public string? Provenance { get; private set; }
    public DateTimeOffset? EffectiveTime { get; private set; }
    public DateTimeOffset ObservedAtUtc { get; private set; }

    private Observation() { }

    internal static Observation Create(
        Ulid treatmentSessionId,
        ObservationCode code,
        string? value,
        string? unit,
        string? subId,
        string? provenance,
        DateTimeOffset? effectiveTime)
    {
        return new Observation
        {
            TreatmentSessionId = treatmentSessionId,
            Code = code,
            Value = value,
            Unit = unit,
            SubId = subId,
            Provenance = provenance,
            EffectiveTime = effectiveTime,
            ObservedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
