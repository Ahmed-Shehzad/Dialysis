using Dialysis.Persistence;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Cohorts;

public sealed class QueryCohortQueryHandler : IQueryHandler<QueryCohortQuery, CohortResultDto>
{
    private readonly DialysisDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QueryCohortQueryHandler(DialysisDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<CohortResultDto> HandleAsync(QueryCohortQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var patients = await _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.FamilyName)
            .ThenBy(p => p.LogicalId)
            .Take(1000)
            .Select(p => p.LogicalId.Value)
            .ToListAsync(cancellationToken);

        var resultIds = new List<string>();

        if (request.HasActiveAlert == true)
        {
            foreach (var pid in patients)
            {
                var hasAlert = await _db.Alerts
                    .AsNoTracking()
                    .AnyAsync(a => a.TenantId == tenantId && a.PatientId.Value == pid && a.Status == Domain.Entities.AlertStatus.Active, cancellationToken);
                if (hasAlert)
                    resultIds.Add(pid);
            }
        }
        else if (request.SessionFrom.HasValue || request.SessionTo.HasValue)
        {
            foreach (var pid in patients)
            {
                var sessions = await _db.Sessions
                    .AsNoTracking()
                    .Where(s => s.TenantId == tenantId && s.PatientId.Value == pid)
                    .OrderByDescending(s => s.StartedAt)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                var matches = sessions.Any(s =>
                {
                    var started = s.StartedAt.UtcDateTime;
                    if (request.SessionFrom.HasValue && started < request.SessionFrom.Value) return false;
                    if (request.SessionTo.HasValue && started > request.SessionTo.Value) return false;
                    return true;
                });
                if (matches)
                    resultIds.Add(pid);
            }
        }
        else
        {
            resultIds = patients;
        }

        var paged = resultIds.Skip(request.Offset).Take(Math.Min(request.Limit, 500)).ToList();
        return new CohortResultDto(paged, resultIds.Count);
    }
}
