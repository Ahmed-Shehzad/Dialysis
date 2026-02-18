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

    public Observation AddObservation(
        ObservationCode code,
        string? value,
        string? unit,
        string? subId,
        string? provenance,
        DateTimeOffset? effectiveTime)
    {
        var observation = Observation.Create(Id, code, value, unit, subId, provenance, effectiveTime);
        _observations.Add(observation);
        ApplyEvent(new ObservationRecordedEvent(Id, observation.Id, code, value, unit));
        return observation;
    }

    public void UpdateContext(MedicalRecordNumber? patientMrn, DeviceId? deviceId)
    {
        if (patientMrn is not null && PatientMrn != patientMrn)
            PatientMrn = patientMrn;
        if (deviceId is not null && DeviceId != deviceId)
            DeviceId = deviceId;
    }

    public void Complete()
    {
        Status = TreatmentSessionStatus.Completed;
        EndedAt = DateTimeOffset.UtcNow;
        ApplyUpdateDateTime();
        ApplyEvent(new TreatmentSessionCompletedEvent(Id, SessionId));
    }
}
