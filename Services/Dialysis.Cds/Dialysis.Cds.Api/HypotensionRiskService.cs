using Hl7.Fhir.Model;

namespace Dialysis.Cds.Api;

/// <summary>
/// Detects hypotension risk from blood pressure observations.
/// Thresholds: systolic &lt; 90 mmHg, diastolic &lt; 60 mmHg.
/// </summary>
public sealed class HypotensionRiskService
{
    private const decimal SystolicThreshold = 90m;
    private const decimal DiastolicThreshold = 60m;
    private const string SystolicCode = "MDC_PRESS_BLD_SYS";
    private const string DiastolicCode = "MDC_PRESS_BLD_DIA";

    /// <summary>
    /// Evaluates observations for hypotension. Returns DetectedIssue if any BP reading below threshold.
    /// </summary>
    public DetectedIssue? Evaluate(string sessionId, string? patientMrn, IReadOnlyList<ObservationDto> observations)
    {
        var issues = new List<DetectedIssue.EvidenceComponent>();

        decimal? systolic = GetNumericValue(observations, SystolicCode);
        if (systolic.HasValue && systolic.Value < SystolicThreshold)
            issues.Add(new DetectedIssue.EvidenceComponent
            {
                Detail = [new ResourceReference($"Observation?code={SystolicCode}&subject=Patient/{patientMrn ?? "unknown"}")],
                Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Hypotension")]
            });

        decimal? diastolic = GetNumericValue(observations, DiastolicCode);
        if (diastolic.HasValue && diastolic.Value < DiastolicThreshold)
            issues.Add(new DetectedIssue.EvidenceComponent
            {
                Detail = [new ResourceReference($"Observation?code={DiastolicCode}&subject=Patient/{patientMrn ?? "unknown"}")],
                Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Hypotension")]
            });

        if (issues.Count == 0)
            return null;

        return new DetectedIssue
        {
            Id = $"cds-hypotension-{sessionId}",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Hypotension risk detected"),
            Detail = $"Treatment session {sessionId}: blood pressure below threshold (systolic < {SystolicThreshold} or diastolic < {DiastolicThreshold} mmHg)",
            Evidence = issues,
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
