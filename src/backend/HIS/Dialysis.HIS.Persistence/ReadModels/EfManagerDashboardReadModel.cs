using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.PatientFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.ReadModels;

public sealed class EfManagerDashboardReadModel(HisDbContext db) : IManagerDashboardReadModel
{
    public async Task<ManagerDashboardDto> GetAsync(string? reportFocus, CancellationToken cancellationToken = default)
    {
        var inHouse = await db.Patients.CountAsync(p => p.VisitState == PatientVisitState.InHouse, cancellationToken).ConfigureAwait(false);
        var exports = await db.BillingExportJobs.CountAsync(cancellationToken).ConfigureAwait(false);
        var imports = await db.DataImportJobs.CountAsync(j => j.StatusCode == "Queued", cancellationToken).ConfigureAwait(false);
        var queuedExports = await db.BillingExportJobs.CountAsync(j => j.StatusCode == "Queued", cancellationToken).ConfigureAwait(false);
        var openQuality = await db.RaQualityWorkflowTasks.CountAsync(j => j.StatusCode == "open", cancellationToken).ConfigureAwait(false);
        var focus = string.IsNullOrWhiteSpace(reportFocus) ? null : reportFocus.Trim();
        return new ManagerDashboardDto(inHouse, exports, imports, queuedExports, openQuality, focus);
    }
}
