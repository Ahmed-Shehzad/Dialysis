using BuildingBlocks;

using Dialysis.Contracts.Events;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Domain.Aggregates;

/// <summary>
/// Domain aggregate root for an observation. Inherits AggregateRoot per DDD.
/// </summary>
public sealed class Observation : AggregateRoot
{
    public TenantId TenantId { get; private set; }
    public PatientId PatientId { get; private set; }
    public LoincCode LoincCode { get; private set; }
    public string? Display { get; private set; }
    public UnitOfMeasure? Unit { get; private set; }
    public decimal? NumericValue { get; private set; }
    public ObservationEffective Effective { get; private set; }

    private Observation()
    {
        TenantId = null!;
        PatientId = null!;
        LoincCode = null!;
        Effective = null!;
    }

    public static Observation Create(
        TenantId tenantId,
        PatientId patientId,
        LoincCode loincCode,
        string? display,
        UnitOfMeasure? unit,
        decimal? numericValue,
        ObservationEffective effective)
    {
        var observation = new Observation
        {
            TenantId = tenantId,
            PatientId = patientId,
            LoincCode = loincCode,
            Display = display,
            Unit = unit,
            NumericValue = numericValue,
            Effective = effective
        };

        observation.ApplyEvent(new ObservationCreated(
            new ObservationId(observation.Id.ToString()),
            patientId,
            tenantId,
            loincCode,
            unit,
            numericValue,
            effective));

        return observation;
    }
}
