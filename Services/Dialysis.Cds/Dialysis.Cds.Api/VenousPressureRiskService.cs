using Hl7.Fhir.Model;

namespace Dialysis.Cds.Api;

/// <summary>
/// Detects high venous pressure risk from observations (&gt; 200 mmHg).
/// </summary>
public sealed class VenousPressureRiskService
{
    private const decimal HighVenousPressureThreshold = 200m;
    private const string VenousPressureCode = "MDC_PRESS_BLD_VEN";

    public DetectedIssue? Evaluate(string sessionId, string? patientMrn, IReadOnlyList<ObservationDto> observations)
    {
        decimal? value = GetNumericValue(observations, VenousPressureCode);
        if (!value.HasValue || value.Value <= HighVenousPressureThreshold)
            return null;

        return new DetectedIssue
        {
            Id = $"cds-venous-pressure-{sessionId}",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "High venous pressure"),
            Detail = $"Treatment session {sessionId}: venous pressure {value} mmHg exceeds threshold {HighVenousPressureThreshold} mmHg",
            Evidence =
            [
                new DetectedIssue.EvidenceComponent
                {
                    Detail = [new ResourceReference($"Observation?code={VenousPressureCode}&subject=Patient/{patientMrn ?? "unknown"}")],
                    Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "High venous pressure")]
                }
            ],
            Identified = new FhirDateTime(DateTimeOffset.UtcNow)
        };
    }

    private static decimal? GetNumericValue(IReadOnlyList<ObservationDto> observations, string code)
    {
        ObservationDto? obs = observations.FirstOrDefault(o => o.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (obs?.Value is null)
            return null;
        return decimal.TryParse(obs.Value, out decimal v) ? v : null;
    }
}
