using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// RA Fig. 6 — Generic MIS → Reporting. Aggregates facility-operations counts across the HIS DbContext.
/// </summary>
public sealed class EfManagerDashboardReadModel : IManagerDashboardReadModel
{
    private readonly HisDbContext _db;
    /// <summary>
    /// RA Fig. 6 — Generic MIS → Reporting. Aggregates facility-operations counts across the HIS DbContext.
    /// </summary>
    public EfManagerDashboardReadModel(HisDbContext db) => _db = db;
    public async Task<ManagerDashboardSnapshotDto> SnapshotAsync(string? reportFocus, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);

        var queuedSpec = new BillingExportJobByStatusSpecification(BillingExportJobStatus.Queued);
        var queuedBilling = await _db.BillingExportJobs
            .AsNoTracking()
            .Where(queuedSpec.ToExpression())
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var openQuality = await _db.RaQualityWorkflowTasks
            .AsNoTracking()
            .CountAsync(t => t.ClosedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        var recentImports = await _db.DataImportJobs
            .AsNoTracking()
            .CountAsync(j => j.SubmittedAtUtc >= since, cancellationToken)
            .ConfigureAwait(false);

        return new ManagerDashboardSnapshotDto(
            ReportFocus: reportFocus,
            QueuedBillingExportJobsCount: queuedBilling,
            OpenQualityWorkflowTasksCount: openQuality,
            RecentImportJobsCount: recentImports,
            GeneratedAtUtc: DateTime.UtcNow);
    }
}
