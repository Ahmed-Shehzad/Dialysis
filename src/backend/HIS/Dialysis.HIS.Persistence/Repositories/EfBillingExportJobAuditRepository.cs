using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// Repository for audit rows projected from <c>BillingExportJobQueuedDomainEvent</c>. The handler runs
/// post-SaveChanges (via <see cref="Dialysis.DomainDrivenDesign.DomainEvents.DomainEventSaveChangesInterceptor"/>),
/// so this write happens in its own transaction by calling <c>SaveChangesAsync</c> directly.
/// </summary>
public sealed class EfBillingExportJobAuditRepository : IBillingExportJobAuditRepository
{
    private readonly HisDbContext _db;
    /// <summary>
    /// Repository for audit rows projected from <c>BillingExportJobQueuedDomainEvent</c>. The handler runs
    /// post-SaveChanges (via <see cref="Dialysis.DomainDrivenDesign.DomainEvents.DomainEventSaveChangesInterceptor"/>),
    /// so this write happens in its own transaction by calling <c>SaveChangesAsync</c> directly.
    /// </summary>
    public EfBillingExportJobAuditRepository(HisDbContext db) => _db = db;
    public async Task RecordAsync(BillingExportJobAudit audit, CancellationToken cancellationToken = default)
    {
        _db.BillingExportJobAudits.Add(audit);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
