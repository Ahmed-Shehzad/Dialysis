using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Abstractions;

public interface ITreatmentSessionRepository : IRepository<TreatmentSession>
{
    Task<TreatmentSession?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<TreatmentSession> GetOrCreateAsync(SessionId sessionId, MedicalRecordNumber? patientMrn, DeviceId? deviceId, CancellationToken cancellationToken = default);
}
