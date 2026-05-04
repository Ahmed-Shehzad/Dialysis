using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Medication.Features.DiscontinueMedicationOrder;

public sealed record DiscontinueMedicationOrderCommand(Guid OrderId)
    : ICommand<Unit>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.MedicationOrderDiscontinue;
}
