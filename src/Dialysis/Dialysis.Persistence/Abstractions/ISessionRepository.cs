using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.Persistence.Abstractions;

public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken cancellationToken = default);
    Task<Session?> GetByIdAsync(TenantId tenantId, SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Session>> GetByPatientAsync(TenantId tenantId, PatientId patientId, int? limit = null, int offset = 0, CancellationToken cancellationToken = default);
}
