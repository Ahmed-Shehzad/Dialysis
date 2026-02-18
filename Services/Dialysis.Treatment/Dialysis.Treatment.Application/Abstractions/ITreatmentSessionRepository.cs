using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Abstractions;

public interface ITreatmentSessionRepository
{
    Task<TreatmentSession?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<TreatmentSession> GetOrCreateAsync(SessionId sessionId, MedicalRecordNumber? patientMrn, DeviceId? deviceId, CancellationToken cancellationToken = default);
    Task SaveAsync(TreatmentSession session, CancellationToken cancellationToken = default);
}

public sealed record ObservationInfo(
    ObservationCode Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? Provenance,
    DateTimeOffset? EffectiveTime);

public sealed record OruParseResult(
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId,
    IReadOnlyList<ObservationInfo> Observations);
