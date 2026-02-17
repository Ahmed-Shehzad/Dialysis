using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;

namespace Dialysis.Persistence;

public sealed class SessionRepository : ISessionRepository
{
    private readonly DialysisDbContext _db;

    public SessionRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}
