namespace Dialysis.HIS.DataServices.Ports;

/// <summary>
/// RA Fig. 6 — Generic MIS → Reporting. Counts roll up facility-operations workload without leaking PHI.
/// </summary>
public sealed record ManagerDashboardSnapshotDto
{
    /// <summary>
    /// RA Fig. 6 — Generic MIS → Reporting. Counts roll up facility-operations workload without leaking PHI.
    /// </summary>
    public ManagerDashboardSnapshotDto(string? ReportFocus,
        int QueuedBillingExportJobsCount,
        int OpenQualityWorkflowTasksCount,
        int RecentImportJobsCount,
        DateTime GeneratedAtUtc)
    {
        this.ReportFocus = ReportFocus;
        this.QueuedBillingExportJobsCount = QueuedBillingExportJobsCount;
        this.OpenQualityWorkflowTasksCount = OpenQualityWorkflowTasksCount;
        this.RecentImportJobsCount = RecentImportJobsCount;
        this.GeneratedAtUtc = GeneratedAtUtc;
    }
    public string? ReportFocus { get; init; }
    public int QueuedBillingExportJobsCount { get; init; }
    public int OpenQualityWorkflowTasksCount { get; init; }
    public int RecentImportJobsCount { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public void Deconstruct(out string? ReportFocus, out int QueuedBillingExportJobsCount, out int OpenQualityWorkflowTasksCount, out int RecentImportJobsCount, out DateTime GeneratedAtUtc)
    {
        ReportFocus = this.ReportFocus;
        QueuedBillingExportJobsCount = this.QueuedBillingExportJobsCount;
        OpenQualityWorkflowTasksCount = this.OpenQualityWorkflowTasksCount;
        RecentImportJobsCount = this.RecentImportJobsCount;
        GeneratedAtUtc = this.GeneratedAtUtc;
    }
}

public interface IManagerDashboardReadModel
{
    Task<ManagerDashboardSnapshotDto> SnapshotAsync(string? reportFocus, CancellationToken cancellationToken = default);
}
