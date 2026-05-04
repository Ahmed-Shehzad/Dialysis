namespace Dialysis.HIS.DataServices.Ports;

public sealed record ManagerDashboardDto(
    int ActiveInHousePatients,
    int OpenBillingExports,
    int QueuedImportJobs,
    int QueuedBillingExports,
    int OpenQualityWorkflowTasks,
    string? AppliedReportFocus);

public interface IManagerDashboardReadModel
{
    Task<ManagerDashboardDto> GetAsync(string? reportFocus, CancellationToken cancellationToken = default);
}
