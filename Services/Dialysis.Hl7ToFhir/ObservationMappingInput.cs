namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Groups all parameters needed to map a PCD-01 observation to FHIR Observation.
/// Aligns with Treatment service ObservationInfo / Observation entity.
/// </summary>
public sealed record ObservationMappingInput(
    string ObservationCode,
    string? Value,
    string? Unit,
    string? SubId,
    string? ReferenceRange,
    string? Provenance,
    DateTimeOffset? EffectiveTime,
    string? DeviceId,
    string? PatientId);
