using Hl7.Fhir.Model;

namespace Dialysis.Cds.Api;

/// <summary>
/// Compares treatment session observations with prescription; returns DetectedIssue on deviation.
/// Tolerance: ±10% for blood flow and UF rate; ±10% for UF target vs actual removed.
/// </summary>
public sealed class PrescriptionComplianceService
{
    private const decimal ToleranceFactor = 0.10m;
    private const string BloodFlowCode = "MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE";
    private const string UfRateCode = "MDC_HDIALY_UF_RATE";
    private const string UfActualRemovedCode = "MDC_HDIALY_UF_ACTUAL_REMOVED_VOL";

    /// <summary>
    /// Evaluates prescription vs treatment. Returns DetectedIssue if deviation exceeds tolerance.
    /// </summary>
    public DetectedIssue? Evaluate(string sessionId, string? patientMrn, IReadOnlyList<ObservationDto> observations, PrescriptionDto? prescription)
    {
        if (prescription is null || string.IsNullOrWhiteSpace(patientMrn))
            return null;

        var issues = new List<DetectedIssue.EvidenceComponent>();

        decimal? bloodFlowRx = prescription.BloodFlowRateMlMin;
        decimal? bloodFlowTx = GetNumericValue(observations, BloodFlowCode);
        if (bloodFlowRx.HasValue && bloodFlowTx.HasValue && !WithinTolerance(bloodFlowTx.Value, bloodFlowRx.Value))
            issues.Add(new DetectedIssue.EvidenceComponent { Detail = [new ResourceReference($"Observation?code={BloodFlowCode}&subject=Patient/{patientMrn}")], Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Deviation")] });

        decimal? ufRateRx = prescription.UfRateMlH;
        decimal? ufRateTx = GetNumericValue(observations, UfRateCode);
        if (ufRateRx.HasValue && ufRateTx.HasValue && !WithinTolerance(ufRateTx.Value, ufRateRx.Value))
            issues.Add(new DetectedIssue.EvidenceComponent { Detail = [new ResourceReference($"Observation?code={UfRateCode}&subject=Patient/{patientMrn}")], Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Deviation")] });

        decimal? ufTargetRx = prescription.UfTargetVolumeMl;
        decimal? ufActualTx = GetNumericValue(observations, UfActualRemovedCode);
        if (ufTargetRx.HasValue && ufActualTx.HasValue && !WithinTolerance(ufActualTx.Value, ufTargetRx.Value))
            issues.Add(new DetectedIssue.EvidenceComponent { Detail = [new ResourceReference($"Observation?code={UfActualRemovedCode}&subject=Patient/{patientMrn}")], Code = [new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Deviation")] });

        if (issues.Count == 0)
            return null;

        return new DetectedIssue
        {
            Id = $"cds-{sessionId}",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v3-ActCode", "DEVV", "Prescription deviation"),
            Detail = $"Treatment session {sessionId} deviates from prescription for patient {patientMrn}",
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

    private static bool WithinTolerance(decimal actual, decimal prescribed) =>
        prescribed == 0 || Math.Abs(actual - prescribed) / prescribed <= ToleranceFactor;
}

public sealed record ObservationDto(string Code, string? Value, string? Unit);
public sealed record PrescriptionDto(decimal? BloodFlowRateMlMin, decimal? UfRateMlH, decimal? UfTargetVolumeMl);
