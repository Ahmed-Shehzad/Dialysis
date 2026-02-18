namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Groups all parameters needed to map a PCD-04 alarm to FHIR DetectedIssue.
/// Aligns with Alarm service AlarmInfo. InterpretationType = OBX-8 SP/ST/SA (system/technical/advisory).
/// </summary>
public sealed record AlarmMappingInput(
    string? AlarmType,
    string? SourceCode,
    string? SourceLimits,
    string EventPhase,
    string AlarmState,
    string ActivityState,
    string? AlarmPriority,
    string? InterpretationType,
    string? DisplayName,
    string? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt);
