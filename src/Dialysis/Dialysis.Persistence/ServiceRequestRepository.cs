using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly DialysisDbContext _db;

    public ServiceRequestRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(ServiceRequest order, CancellationToken cancellationToken = default)
    {
        await _db.ServiceRequests.AddAsync(order, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ServiceRequest?> GetAsync(TenantId tenantId, string id, CancellationToken cancellationToken = default)
        => await _db.ServiceRequests
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantId && o.Id.ToString() == id,
                cancellationToken);

    public async Task<IReadOnlyList<ServiceRequest>> ListByPatientAsync(
        TenantId tenantId,
        PatientId patientId,
        string? status = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ServiceRequests
            .Where(o => o.TenantId == tenantId && o.PatientId == patientId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        return await query
            .OrderByDescending(o => o.AuthoredOn)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
