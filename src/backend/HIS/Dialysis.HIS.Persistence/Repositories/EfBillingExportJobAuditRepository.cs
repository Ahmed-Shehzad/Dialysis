using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// Repository for audit rows projected from <c>BillingExportJobQueuedDomainEvent</c>. The handler runs
/// post-SaveChanges (via <see cref="Dialysis.DomainDrivenDesign.DomainEvents.DomainEventSaveChangesInterceptor"/>),
/// so this write happens in its own transaction by calling <c>SaveChangesAsync</c> directly.
/// </summary>
public sealed class EfBillingExportJobAuditRepository(HisDbContext db) : IBillingExportJobAuditRepository
{
    public async Task RecordAsync(BillingExportJobAudit audit, CancellationToken cancellationToken = default)
    {
        db.BillingExportJobAudits.Add(audit);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
