using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;

public sealed record EnqueueWaitlistEntryCommand(
    Guid PatientId,
    string ResourceKindCode,
    string Notes,
    DateTime RequestedNotBeforeUtc) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
