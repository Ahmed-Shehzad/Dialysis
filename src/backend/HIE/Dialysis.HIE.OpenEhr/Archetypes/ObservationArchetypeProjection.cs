using System.Text.Json;
using Dialysis.HIE.Core.Abstraction.OpenEhr;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.OpenEhr.Archetypes;

/// <summary>
/// Projects a lab-result FHIR <see cref="Observation"/> to the openEHR
/// OBSERVATION.lab_test_result.v1 shape: code (LOINC), value, unit, range, interpretation, time.
/// </summary>
public sealed class ObservationArchetypeProjection : IArchetypeProjection<Observation>
{
    public string ArchetypeId => ArchetypeIds.LabTestResult;

    public string Project(Observation resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var code = resource.Code?.Coding.FirstOrDefault();
        var quantity = resource.Value as Quantity;
        var stringValue = resource.Value as FhirString;
        var effective = resource.Effective is FhirDateTime fdt ? fdt.Value : null;

        var payload = new
        {
            archetype_node_id = ArchetypeIds.LabTestResult,
            data = new
            {
                test = new
                {
                    code = code?.Code,
                    system = code?.System,
                    display = code?.Display,
                },
                value = new
                {
                    magnitude = quantity?.Value,
                    unit = quantity?.Unit,
                    unit_system = quantity?.System,
                    text = stringValue?.Value,
                },
                reference_range = resource.ReferenceRange.Select(r => r.Text).Where(t => !string.IsNullOrWhiteSpace(t)),
                interpretation = resource.Interpretation.SelectMany(i => i.Coding).Select(c => c.Code),
                effective_time = effective,
                status = resource.Status?.ToString(),
            },
        };
        return JsonSerializer.Serialize(payload);
    }
}
