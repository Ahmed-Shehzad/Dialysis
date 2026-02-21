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
/// <param name="SignedAt">Session sign time (when signed by clinician).</param>
/// <param name="SignedBy">Clinician identifier who signed the session.</param>
/// <param name="PreAssessment">Pre-treatment assessment when recorded.</param>
public sealed record GetTreatmentSessionResponse(
    string SessionId,
    string? PatientMrn,
    string? DeviceId,
    string? DeviceEui64,
    string? TherapyId,
    string Status,
    DateTimeOffset? StartedAt,
    IReadOnlyList<ObservationDto> Observations,
    DateTimeOffset? EndedAt = null,
    DateTimeOffset? SignedAt = null,
    string? SignedBy = null,
    PreAssessmentResponse? PreAssessment = null);

public sealed record PreAssessmentResponse(
    decimal? PreWeightKg,
    int? BpSystolic,
    int? BpDiastolic,
    string? AccessTypeValue,
    bool PrescriptionConfirmed,
    string? PainSymptomNotes,
    DateTimeOffset RecordedAt,
    string? RecordedBy);

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
