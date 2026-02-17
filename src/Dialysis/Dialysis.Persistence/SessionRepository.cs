using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class SessionRepository : ISessionRepository
{
    private readonly DialysisDbContext _db;

    public SessionRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Session?> GetByIdAsync(TenantId tenantId, SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.Sessions
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.Id.ToString() == sessionId.Value,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByPatientAsync(TenantId tenantId, PatientId patientId, int? limit = null, int offset = 0, CancellationToken cancellationToken = default)
    {
        IQueryable<Session> query = _db.Sessions
            .Where(s => s.TenantId == tenantId && s.PatientId == patientId)
            .OrderByDescending(s => s.StartedAt)
            .Skip(offset);
        if (limit.HasValue)
            query = query.Take(limit.Value);
        return await query.ToListAsync(cancellationToken);
    }
}
