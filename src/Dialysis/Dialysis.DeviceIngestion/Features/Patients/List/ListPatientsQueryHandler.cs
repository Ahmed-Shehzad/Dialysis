using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.DeviceIngestion.Features.Patients.List;

public sealed class ListPatientsQueryHandler : IQueryHandler<ListPatientsQuery, IReadOnlyList<Patient>>
{
    private readonly DialysisDbContext _db;

    public ListPatientsQueryHandler(DialysisDbContext db) => _db = db;

    public async Task<IReadOnlyList<Patient>> HandleAsync(ListPatientsQuery request, CancellationToken cancellationToken = default)
    {
        var query = _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId);

        if (!string.IsNullOrWhiteSpace(request.Family))
            query = query.Where(p => p.FamilyName != null && p.FamilyName.ToLower().Contains(request.Family.ToLower()));
        if (!string.IsNullOrWhiteSpace(request.Given))
            query = query.Where(p => p.GivenNames != null && p.GivenNames.ToLower().Contains(request.Given.ToLower()));

        return await query
            .OrderBy(p => p.FamilyName)
            .ThenBy(p => p.LogicalId)
            .Skip(request.Offset)
            .Take(request.Count ?? 1000)
            .ToListAsync(cancellationToken);
    }
}
