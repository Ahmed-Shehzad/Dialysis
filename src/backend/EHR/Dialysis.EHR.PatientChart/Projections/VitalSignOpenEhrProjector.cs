using System.Text.Json;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Projections;

/// <summary>
/// Projects a <see cref="VitalSignReading"/> into the matching openEHR archetype shape.
/// Returns the archetype id + canonical-JSON payload, or <c>null</c> when the LOINC code has no
/// registered archetype mapping (caller skips the parallel openEHR publish in that case).
/// </summary>
public sealed class VitalSignOpenEhrProjector
{
    public OpenEhrProjection? Project(VitalSignReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var archetypeId = ArchetypeFor(reading.ObservationType.Code);
        if (archetypeId is null) return null;

        var payload = JsonSerializer.Serialize(new
        {
            archetype_node_id = archetypeId,
            data = new
            {
                observation = new
                {
                    code = reading.ObservationType.Code,
                    system = reading.ObservationType.System,
                    display = reading.ObservationType.Display,
                },
                value = new
                {
                    magnitude = reading.Value,
                    unit = reading.UnitCode,
                },
                effective_time = reading.ObservedAtUtc,
                encounter_id = reading.EncounterId,
                recorded_by_provider_id = reading.RecordedByProviderId,
            },
        });

        return new OpenEhrProjection(archetypeId, payload);
    }

    private static string? ArchetypeFor(string loincCode) => loincCode switch
    {
        EhrLoincCodes.BloodPressurePanel or
            EhrLoincCodes.SystolicBloodPressure or
            EhrLoincCodes.DiastolicBloodPressure => OpenEhrArchetypes.BloodPressure,
        EhrLoincCodes.BodyWeight => OpenEhrArchetypes.BodyWeight,
        EhrLoincCodes.BodyHeight => OpenEhrArchetypes.BodyHeight,
        EhrLoincCodes.PulseRate => OpenEhrArchetypes.PulseRate,
        EhrLoincCodes.BodyTemperature => OpenEhrArchetypes.BodyTemperature,
        _ => null,
    };
}

public sealed record OpenEhrProjection(string ArchetypeId, string CompositionJson);
