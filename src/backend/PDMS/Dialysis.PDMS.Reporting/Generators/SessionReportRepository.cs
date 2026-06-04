using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Reporting.Domain;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// <see cref="ISessionReportRepository"/> over the shared <see cref="IPdmsRepository{TAggregate,TId}"/>
/// registration (in-memory in dev/tests, EF + Postgres in hosted environments) — the same store the
/// read-only <c>ReportsController</c> queries, so written reports are immediately visible there.
/// </summary>
public sealed class SessionReportRepository : ISessionReportRepository
{
    private readonly IPdmsRepository<SessionReport, Guid> _reports;

    /// <summary>Creates the repository.</summary>
    public SessionReportRepository(IPdmsRepository<SessionReport, Guid> reports) => _reports = reports;

    /// <inheritdoc />
    public Task AddAsync(SessionReport report, CancellationToken cancellationToken) =>
        _reports.AddAsync(report, cancellationToken);

    /// <inheritdoc />
    public Task<SessionReport?> FindAsync(Guid reportId, CancellationToken cancellationToken) =>
        _reports.GetByIdAsync(reportId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionReport>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // Report volumes are tiny (≤ one row per kind per session), so an all-rows fetch + filter
        // is cheaper than a bespoke specification and stays provider-agnostic.
        var all = await _reports.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return [.. all.Where(r => r.SessionId == sessionId)];
    }
}
