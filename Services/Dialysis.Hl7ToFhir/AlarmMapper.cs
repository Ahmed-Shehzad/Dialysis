using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps PCD-04 alarms to FHIR DetectedIssue.
/// Alarm priority (PH/PM/PL from OBX-8) maps to DetectedIssue.severity.
/// Clinical alarms requiring action map to DetectedIssue.
/// </summary>
public static class AlarmMapper
{
    private const string MdcSystem = "urn:iso:std:iso:11073:10101";

    /// <summary>
    /// Map a PCD-04 alarm to FHIR DetectedIssue.
    /// </summary>
    public static DetectedIssue ToFhirDetectedIssue(AlarmMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        DetectedIssue.DetectedIssueSeverity severity = MapPriorityToSeverity(input.AlarmPriority);

        string displayName = input.DisplayName ?? input.AlarmType ?? "Alarm";
        string detail = $"[{input.EventPhase}] {input.AlarmState} / {input.ActivityState}";
        if (!string.IsNullOrEmpty(input.AlarmType))
            detail = $"{displayName}: {detail}";
        if (!string.IsNullOrEmpty(input.SourceLimits))
            detail += $" | Source/Limits: {input.SourceLimits}";

        string codeValue = input.SourceCode ?? input.AlarmType ?? "alarm";

        return new DetectedIssue
        {
            Status = ObservationStatus.Final,
            Code = new CodeableConcept
            {
                Coding = [new Coding(MdcSystem, codeValue, displayName)],
                Text = displayName
            },
            Severity = severity,
            Detail = detail,
            Identified = new FhirDateTime(input.OccurredAt),
            Evidence = !string.IsNullOrEmpty(input.DeviceId)
                ? [new DetectedIssue.EvidenceComponent { Detail = [new ResourceReference($"Device/{input.DeviceId}")] }]
                : []
        };
    }

    /// <summary>
    /// Map OBX-8 alarm priority (PH/PM/PL) to DetectedIssue.severity.
    /// </summary>
    private static DetectedIssue.DetectedIssueSeverity MapPriorityToSeverity(string? priority)
    {
        if (string.IsNullOrEmpty(priority)) return DetectedIssue.DetectedIssueSeverity.Moderate;

        return priority.ToUpperInvariant() switch
        {
            "PH" => DetectedIssue.DetectedIssueSeverity.High,
            "PM" => DetectedIssue.DetectedIssueSeverity.Moderate,
            "PL" => DetectedIssue.DetectedIssueSeverity.Low,
            "PI" => DetectedIssue.DetectedIssueSeverity.Low,
            "PN" => DetectedIssue.DetectedIssueSeverity.Low,
            _ => DetectedIssue.DetectedIssueSeverity.Moderate
        };
    }
}
