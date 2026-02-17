using Dialysis.Persistence;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Services;

/// <summary>
/// Builds de-identified quality bundles using DbContext directly with AsNoTracking.
/// </summary>
public sealed class QualityBundleService : IQualityBundleService
{
    private readonly DialysisDbContext _db;

    public QualityBundleService(DialysisDbContext db) => _db = db;

    public async Task<QualityBundleResult> GetDeidentifiedBundleAsync(string tenantId, DateTime from, DateTime to, int limit, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var fromOffset = new DateTimeOffset(from, TimeSpan.Zero);
        var toOffset = new DateTimeOffset(to, TimeSpan.Zero);

        var allSessions = new List<Domain.Aggregates.Session>();
        var patientList = await _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == tenant)
            .OrderBy(p => p.FamilyName)
            .ThenBy(p => p.LogicalId)
            .Take(1000)
            .ToListAsync(cancellationToken);

        foreach (var p in patientList)
        {
            var sessions = await _db.Sessions
                .AsNoTracking()
                .Where(s => s.TenantId == tenant && s.PatientId == p.LogicalId && s.StartedAt >= fromOffset && s.StartedAt <= toOffset)
                .OrderByDescending(s => s.StartedAt)
                .Take(200)
                .ToListAsync(cancellationToken);

            if (sessions.Count > 0)
            {
                allSessions.AddRange(sessions.Take(limit - allSessions.Count));
            }
            if (allSessions.Count >= limit)
                break;
        }

        return new QualityBundleResult(allSessions);
    }
}
