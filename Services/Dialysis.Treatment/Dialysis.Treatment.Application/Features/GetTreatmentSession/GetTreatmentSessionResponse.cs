namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

/// <summary>
/// Response containing treatment session details.
/// </summary>
/// <param name="SessionId">Therapy/session identifier.</param>
/// <param name="PatientMrn">Patient MRN when known.</param>
/// <param name="DeviceId">Device identifier.</param>
/// <param name="DeviceEui64">EUI-64 from MSH-3 or OBR-3 when available.</param>
/// <param name="TherapyId">Therapy_ID from OBR-3 (full composite) when available.</param>
/// <param name="Status">Session status (Active, Completed).</param>
/// <param name="StartedAt">Session start time.</param>
/// <param name="Observations">Device observations for FHIR mapping.</param>
/// <param name="EndedAt">Session end time (when completed).</param>
public sealed record GetTreatmentSessionResponse(
    string SessionId,
    string? PatientMrn,
    string? DeviceId,
    string? DeviceEui64,
    string? TherapyId,
    string Status,
    DateTimeOffset? StartedAt,
    IReadOnlyList<ObservationDto> Observations,
    DateTimeOffset? EndedAt = null);

/// <summary>
/// Observation data for FHIR mapping.
/// </summary>
public sealed record ObservationDto(
    string Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? ReferenceRange,
    string? Provenance,
    DateTimeOffset? EffectiveTime,
    string? ChannelName = null);
