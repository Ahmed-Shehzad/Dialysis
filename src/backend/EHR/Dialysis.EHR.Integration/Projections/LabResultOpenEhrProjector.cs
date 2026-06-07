using System.Text.Json;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Integration.Features.IngestLabResult;

namespace Dialysis.EHR.Integration.Projections;

/// <summary>
/// Projects an incoming lab-result command into <c>openEHR-EHR-OBSERVATION.lab_test_result.v1</c>.
/// </summary>
public sealed class LabResultOpenEhrProjector
{
    public static LabResultOpenEhrProjection Project(IngestLabResultCommand request, Guid labResultId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payload = JsonSerializer.Serialize(new
        {
            archetype_node_id = OpenEhrArchetypes.LabTestResult,
            data = new
            {
                test = new
                {
                    code = request.LoincCode,
                    system = EhrCodeSystems.Loinc,
                },
                value = new
                {
                    text = request.ValueText,
                    unit = request.UnitCode,
                },
                reference_range = request.ReferenceRangeText,
                interpretation = request.AbnormalFlagCode,
                effective_time = request.ObservedAtUtc,
                lab_result_id = labResultId,
                lab_order_id = request.LabOrderId,
            },
        });

        return new LabResultOpenEhrProjection(OpenEhrArchetypes.LabTestResult, payload);
    }
}

public sealed record LabResultOpenEhrProjection
{
    public LabResultOpenEhrProjection(string ArchetypeId, string CompositionJson)
    {
        this.ArchetypeId = ArchetypeId;
        this.CompositionJson = CompositionJson;
    }
    public string ArchetypeId { get; init; }
    public string CompositionJson { get; init; }
    public void Deconstruct(out string ArchetypeId, out string CompositionJson)
    {
        ArchetypeId = this.ArchetypeId;
        CompositionJson = this.CompositionJson;
    }
}
