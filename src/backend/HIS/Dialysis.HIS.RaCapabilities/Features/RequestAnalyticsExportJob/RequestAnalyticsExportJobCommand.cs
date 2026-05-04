using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;

public sealed record RequestAnalyticsExportJobCommand(string PipelineCode) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
