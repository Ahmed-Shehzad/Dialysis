using Dialysis.Simulation.Engine.Domain;
using Dialysis.Simulation.Engine.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Simulation.Persistence.Repositories;

/// <summary>EF-backed <see cref="ISimulationSessionRepository"/>.</summary>
public sealed class EfSimulationSessionRepository : ISimulationSessionRepository
{
    private readonly SimulationDbContext _db;

    /// <summary>Creates the repository.</summary>
    public EfSimulationSessionRepository(SimulationDbContext db) => _db = db;

    /// <inheritdoc />
    public void Add(SimulationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _db.SimulationSessions.Add(session);
    }

    /// <inheritdoc />
    public Task<SimulationSession?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _db.SimulationSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}

/// <summary>EF-backed <see cref="ISimulationWriteStore"/> on the same context as the session repository.</summary>
public sealed class EfSimulationWriteStore : ISimulationWriteStore
{
    private readonly SimulationDbContext _db;

    /// <summary>Creates the write store.</summary>
    public EfSimulationWriteStore(SimulationDbContext db) => _db = db;

    /// <inheritdoc />
    public void AppendEvent(SimulationEventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _db.SimulationEvents.Add(record);
    }

    /// <inheritdoc />
    public void AppendAudit(SimulationAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _db.SimulationAuditEntries.Add(entry);
    }

    /// <inheritdoc />
    public void AppendLink(SessionRecordLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        _db.SessionRecordLinks.Add(link);
    }
}

/// <summary>EF-backed read-side projections.</summary>
public sealed class EfSimulationQueryStore : ISimulationQueryStore
{
    private readonly SimulationDbContext _db;

    /// <summary>Creates the query store.</summary>
    public EfSimulationQueryStore(SimulationDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<SimulationSession?> GetSessionAsync(Guid id, CancellationToken cancellationToken) =>
        _db.SimulationSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SimulationEventRecord>> ListEventsAsync(Guid sessionId, int take, CancellationToken cancellationToken)
    {
        var rows = await _db.SimulationEvents
            .AsNoTracking()
            .Where(e => e.SimulationSessionId == sessionId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SimulationAuditEntry>> ListAuditAsync(Guid sessionId, int take, CancellationToken cancellationToken)
    {
        var rows = await _db.SimulationAuditEntries
            .AsNoTracking()
            .Where(a => a.SimulationSessionId == sessionId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }
}
