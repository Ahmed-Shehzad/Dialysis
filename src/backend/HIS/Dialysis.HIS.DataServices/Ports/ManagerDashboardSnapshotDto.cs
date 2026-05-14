namespace Dialysis.HIS.DataServices.Ports;

/// <summary>
/// RA Fig. 6 — Generic MIS → Reporting. Counts roll up facility-operations workload without leaking PHI.
/// </summary>
public sealed record ManagerDashboardSnapshotDto(
    string? ReportFocus,
    int QueuedBillingExportJobsCount,
    int OpenQualityWorkflowTasksCount,
    int RecentImportJobsCount,
    DateTime GeneratedAtUtc);

public interface IManagerDashboardReadModel
{
    Task<ManagerDashboardSnapshotDto> SnapshotAsync(string? reportFocus, CancellationToken cancellationToken = default);
}
