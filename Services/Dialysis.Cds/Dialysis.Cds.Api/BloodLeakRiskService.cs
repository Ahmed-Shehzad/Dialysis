using Hl7.Fhir.Model;

namespace Dialysis.Cds.Api;

/// <summary>
/// Detects blood leak from observations. Positive value or non-zero indicates leak.
/// </summary>
public sealed class BloodLeakRiskService
{
    private const string BloodLeakCode = "MDC_DIA_BLD_LEAK_DETECT";

    public DetectedIssue? Evaluate(string sessionId, string? patientMrn, IReadOnlyList<ObservationDto> observations)
    {
        ObservationDto? obs = observations.FirstOrDefault(o => o.Code.Equals(BloodLeakCode, StringComparison.OrdinalIgnoreCase));
        if (obs is null)
            return null;

        if (string.IsNullOrWhiteSpace(obs.Value))
            return null;

        if (decimal.TryParse(obs.Value, out decimal v) && v <= 0)
            return null;

        if (obs.Value.Equals("false", StringComparison.OrdinalIgnoreCase) || obs.Value.Equals("0", StringComparison.Ordinal))
            return null;

        return new DetectedIssue
        {
            Id = $"cds-blood-leak-{sessionId}",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Blood leak detected"),
            Detail = $"Treatment session {sessionId}: blood leak observation reported (value: {obs.Value})",
            Evidence =
            [
                new DetectedIssue.EvidenceComponent
                {
                    Detail = [new ResourceReference($"Observation?code={BloodLeakCode}&subject=Patient/{patientMrn ?? "unknown"}")],
                    Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Blood leak")]
                }
            ],
            Identified = new FhirDateTime(DateTimeOffset.UtcNow)
        };
    }
}
