namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Groups all parameters needed to map a PCD-04 alarm to FHIR DetectedIssue.
/// </summary>
public sealed record AlarmMappingInput(
    string? AlarmType,
    string? SourceLimits,
    string EventPhase,
    string AlarmState,
    string ActivityState,
    string? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt);
