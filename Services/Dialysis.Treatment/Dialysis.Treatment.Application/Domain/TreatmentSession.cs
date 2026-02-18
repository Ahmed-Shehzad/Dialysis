using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain.Events;
using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain;

public sealed class TreatmentSession : AggregateRoot
{
    private readonly List<Observation> _observations = [];

    public SessionId SessionId { get; private set; }
    public MedicalRecordNumber? PatientMrn { get; private set; }
    public DeviceId? DeviceId { get; private set; }
    public TreatmentSessionStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    // ─── Treatment context derived from HL7 OBX observations ─────────────────
    public ModeOfOperation? Mode { get; private set; }
    public TreatmentModality? Modality { get; private set; }
    public EventPhase? Phase { get; private set; }
    public int? TherapyTimePrescribedMin { get; private set; }
    public int ObservationCount { get; private set; }

    public IReadOnlyCollection<Observation> Observations => _observations.AsReadOnly();

    private TreatmentSession() { }

    public static TreatmentSession Start(SessionId sessionId, MedicalRecordNumber? patientMrn, DeviceId? deviceId)
    {
        var session = new TreatmentSession
        {
            SessionId = sessionId,
            PatientMrn = patientMrn,
            DeviceId = deviceId,
            Status = TreatmentSessionStatus.Active,
            StartedAt = DateTimeOffset.UtcNow
        };

        session.ApplyEvent(new TreatmentSessionStartedEvent(session.Id, sessionId, patientMrn, deviceId));
        return session;
    }

    public Observation AddObservation(ObservationCreateParams createParams)
    {
        var observation = Observation.Create(Id, createParams);

        _observations.Add(observation);
        ObservationCount++;

        UpdateContextFromObservation(createParams.Code, createParams.Value);

        ApplyEvent(new ObservationRecordedEvent(Id, observation.Id, createParams.Code, createParams.Value, createParams.Unit));
        return observation;
    }

    public void UpdateContext(MedicalRecordNumber? patientMrn, DeviceId? deviceId)
    {
        if (patientMrn is not null && PatientMrn != patientMrn)
            PatientMrn = patientMrn;
        if (deviceId is not null && DeviceId != deviceId)
            DeviceId = deviceId;
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
