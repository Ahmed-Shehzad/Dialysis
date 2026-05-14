using Dialysis.HIS.DataServices.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

/// <summary>
/// RA Fig. 6 — Generic MIS → Reporting. Aggregates facility-operations counts across the HIS DbContext.
/// </summary>
public sealed class EfManagerDashboardReadModel(HisDbContext db) : IManagerDashboardReadModel
{
    public async Task<ManagerDashboardSnapshotDto> SnapshotAsync(string? reportFocus, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);

        var queuedBilling = await db.BillingExportJobs
            .AsNoTracking()
            .CountAsync(j => j.StatusCode == "Queued", cancellationToken)
            .ConfigureAwait(false);

        var openQuality = await db.RaQualityWorkflowTasks
            .AsNoTracking()
            .CountAsync(t => t.ClosedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        var recentImports = await db.DataImportJobs
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
