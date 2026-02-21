using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain.Events;
using Dialysis.Treatment.Application.Domain.Services;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Events;

namespace Dialysis.Treatment.Application.Domain;

public sealed class TreatmentSession : AggregateRoot
{
    private readonly List<Observation> _observations = [];

    public TenantId TenantId { get; private set; }
    public SessionId SessionId { get; private set; }
    public MedicalRecordNumber? PatientMrn { get; private set; }
    public DeviceId? DeviceId { get; private set; }
    /// <summary>EUI-64 from MSH-3 or OBR-3 (device identifier).</summary>
    public string? DeviceEui64 { get; private set; }
    /// <summary>Therapy_ID from OBR-3 (full composite: ID^Machine^EUI64).</summary>
    public string? TherapyId { get; private set; }
    public TreatmentSessionStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public string? SignedBy { get; private set; }

    // ─── Treatment context derived from HL7 OBX observations ─────────────────
    public ModeOfOperation? Mode { get; private set; }
    public TreatmentModality? Modality { get; private set; }
    public EventPhase? Phase { get; private set; }
    public int? TherapyTimePrescribedMin { get; private set; }
    public int ObservationCount { get; private set; }

    public IReadOnlyCollection<Observation> Observations => _observations.AsReadOnly();

    private TreatmentSession() { }

    public static TreatmentSession Start(SessionId sessionId, MedicalRecordNumber? patientMrn, DeviceId? deviceId, string? tenantId = null)
    {
        var session = new TreatmentSession
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantId.Default : new TenantId(tenantId),
            SessionId = sessionId,
            PatientMrn = patientMrn,
            DeviceId = deviceId,
            Status = TreatmentSessionStatus.Active,
            StartedAt = DateTimeOffset.UtcNow
        };

        session.ApplyEvent(new TreatmentSessionStartedEvent(session.Id, sessionId, patientMrn, deviceId));
        session.ApplyEvent(new TreatmentSessionStartedFhirNotifyEvent(session.Id, sessionId, patientMrn, deviceId));
        return session;
    }

    public Observation AddObservation(ObservationCreateParams createParams, IReadOnlyList<ThresholdBreach>? thresholdBreaches = null)
    {
        if (Status == TreatmentSessionStatus.Completed)
            throw new InvalidOperationException("Cannot add observations to a completed treatment session.");

        var observation = Observation.Create(Id, createParams);

        _observations.Add(observation);
        ObservationCount++;

        UpdateContextFromObservation(createParams.Code, createParams.Value);

        string? channelName = createParams.SubId is not null && ContainmentPath.TryParse(createParams.SubId) is { } path && path.ChannelId is { } cid
            ? ContainmentPath.GetChannelName(cid)
            : null;
        ApplyEvent(new ObservationRecordedSignalRBroadcastEvent(Id, SessionId.Value, observation.Id, createParams.Code, createParams.Value, createParams.Unit, createParams.SubId, channelName));
        ApplyEvent(new ObservationRecordedFhirNotifyEvent(Id, SessionId.Value, observation.Id, createParams.Code, createParams.Value, createParams.Unit, createParams.SubId, channelName));
        ApplyEvent(new ObservationRecordedIntegrationEvent(Id, SessionId.Value, observation.Id, createParams.Code, createParams.Value, createParams.Unit, createParams.SubId, channelName, TenantId.Value));

        if (thresholdBreaches is { Count: > 0 })
            foreach (ThresholdBreach breach in thresholdBreaches)
            {
                ApplyEvent(new ThresholdBreachDetectedEvent(Id, SessionId.Value, DeviceEui64, observation.Id, breach.ObservationCode, breach));
                ApplyEvent(new ThresholdBreachDetectedIntegrationEvent(
                    Id, SessionId.Value, DeviceEui64, observation.Id, breach.ObservationCode.Value,
                    breach.BreachType.ToString(), breach.ObservedValue, breach.ThresholdValue, breach.Direction.ToString(), TenantId.Value));
            }

        return observation;
    }

    public void UpdateContext(MedicalRecordNumber? patientMrn, DeviceId? deviceId)
    {
        if (patientMrn is not null && PatientMrn != patientMrn)
            PatientMrn = patientMrn;
        if (deviceId is not null && DeviceId != deviceId)
            DeviceId = deviceId;
    }

    public void SetDeviceIdentity(string? deviceEui64, string? therapyId)
    {
        if (!string.IsNullOrEmpty(deviceEui64))
            DeviceEui64 = deviceEui64;
        if (!string.IsNullOrEmpty(therapyId))
            TherapyId = therapyId;
    }

    public void SetPhase(EventPhase phase)
    {
        Phase = phase;
        if (phase == EventPhase.End && Status != TreatmentSessionStatus.Completed)
            Complete();
    }

    public void Complete()
    {
        Status = TreatmentSessionStatus.Completed;
        EndedAt = DateTimeOffset.UtcNow;
        ApplyUpdateDateTime();
        ApplyEvent(new TreatmentSessionCompletedEvent(Id, SessionId));
    }

    public void Sign(string? signedBy = null)
    {
        if (Status != TreatmentSessionStatus.Completed)
            throw new InvalidOperationException("Cannot sign a session that is not completed.");
        if (SignedAt.HasValue)
            return; // Idempotent: already signed

        SignedAt = DateTimeOffset.UtcNow;
        SignedBy = signedBy;
        ApplyUpdateDateTime();
        ApplyEvent(new TreatmentSessionSignedEvent(Id, SessionId, SignedBy));
    }

    private void UpdateContextFromObservation(ObservationCode code, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (code == ObservationCode.ModeOfOperation)
            Mode = new ModeOfOperation(value);
        else if (code == ObservationCode.Modality)
            Modality = new TreatmentModality(value);
        else if (code == ObservationCode.TherapyTimePrescribed && int.TryParse(value, out int therapyMin))
            TherapyTimePrescribedMin = therapyMin;
    }
}
