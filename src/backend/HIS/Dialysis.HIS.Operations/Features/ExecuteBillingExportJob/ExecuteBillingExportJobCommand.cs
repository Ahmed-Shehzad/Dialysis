using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.ExecuteBillingExportJob;

/// <summary>
/// Operator action: (re-)dispatch a queued billing-export job to EHR for assembly. HIS owns the queue;
/// executing re-fires the <c>BillingExportJobQueuedIntegrationEvent</c> so EHR's billing pipeline picks
/// it up, assembles the EDI 837 batch, and reports the outcome back (which flips the job out of Queued).
/// Only jobs still in <c>Queued</c> can be executed.
/// </summary>
public sealed record ExecuteBillingExportJobCommand : ICommand, IPermissionedCommand
{
    public ExecuteBillingExportJobCommand(Guid JobId) => this.JobId = JobId;
    public string RequiredPermission => HisPermissions.DataReport;
    public Guid JobId { get; init; }
    public void Deconstruct(out Guid JobId) => JobId = this.JobId;
}
