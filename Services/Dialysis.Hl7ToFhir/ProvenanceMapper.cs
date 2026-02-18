using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps OBX-17 provenance codes to FHIR Provenance.
/// Codes: AMEAS (auto-measurement), MMEAS (manual), ASET (auto-setting), MSET (manual-setting), RSET (remote-setting).
/// </summary>
public static class ProvenanceMapper
{
    private static readonly IReadOnlyDictionary<string, string> ActivityDisplayMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["AMEAS"] = "Automatic measurement",
        ["MMEAS"] = "Manual measurement",
        ["ASET"] = "Automatic setting",
        ["MSET"] = "Manual setting",
        ["RSET"] = "Remote setting"
    };

    /// <summary>
    /// Create a minimal Provenance resource for an observation.
    /// </summary>
    /// <param name="targetReference">Reference to the Observation (e.g., Observation/123).</param>
    /// <param name="provenanceCode">OBX-17 value: AMEAS, MMEAS, ASET, MSET, RSET.</param>
    /// <param name="occurredAt">When the observation/action occurred.</param>
    /// <param name="agentId">Optional agent (device or user) identifier.</param>
    public static Provenance ToFhirProvenance(
        string targetReference,
        string? provenanceCode,
        DateTimeOffset occurredAt,
        string? agentId = null)
    {
        string display = string.IsNullOrEmpty(provenanceCode)
            ? "Unknown"
            : ActivityDisplayMap.GetValueOrDefault(provenanceCode, provenanceCode);

        var prov = new Provenance
        {
            Target = [new ResourceReference(targetReference)],
            Recorded = occurredAt,
            Activity = new CodeableConcept
            {
                Coding = [new Coding("urn:iso:std:iso:11073:10101", provenanceCode ?? "UNK", display)],
                Text = display
            }
        };

        if (!string.IsNullOrEmpty(agentId))
            prov.Agent.Add(new Provenance.AgentComponent
            {
                Who = new ResourceReference($"Device/{agentId}"),
                Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/provenance-participant-type", "assembler", "Assembler")
            });

        return prov;
    }
}
