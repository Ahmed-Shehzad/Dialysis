using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps PCD-04 alarms to FHIR DetectedIssue.
/// Clinical alarms requiring action map to DetectedIssue; device alerts can use Observation.
/// </summary>
public static class AlarmMapper
{
    /// <summary>
    /// Map a PCD-04 alarm to FHIR DetectedIssue.
    /// </summary>
    public static DetectedIssue ToFhirDetectedIssue(AlarmMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        DetectedIssue.DetectedIssueSeverity severity = input.AlarmState switch
        {
            "active" => DetectedIssue.DetectedIssueSeverity.High,
            "latched" => DetectedIssue.DetectedIssueSeverity.Moderate,
            _ => DetectedIssue.DetectedIssueSeverity.Low
        };

        string detail = $"[{input.EventPhase}] {input.AlarmState} / {input.ActivityState}";
        if (!string.IsNullOrEmpty(input.AlarmType))
            detail = $"{input.AlarmType}: {detail}";
        if (!string.IsNullOrEmpty(input.SourceLimits))
            detail += $" | Source/Limits: {input.SourceLimits}";

        return new DetectedIssue
        {
            Status = Hl7.Fhir.Model.ObservationStatus.Final,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding("urn:iso:std:iso:11073:10101", input.AlarmType ?? "alarm", null)
                ]
            },
            Severity = severity,
            Detail = detail,
            Identified = new FhirDateTime(input.OccurredAt),
            Evidence = !string.IsNullOrEmpty(input.DeviceId)
                ? [new DetectedIssue.EvidenceComponent { Detail = [new ResourceReference($"Device/{input.DeviceId}")] }]
                : []
        };
    }
}
