using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;

public sealed record RequestAnalyticsExportJobCommand : ICommand<Guid>, IPermissionedCommand
{
    public RequestAnalyticsExportJobCommand(string PipelineCode) => this.PipelineCode = PipelineCode;
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public string PipelineCode { get; init; }
    public void Deconstruct(out string pipelineCode) => pipelineCode = PipelineCode;
}
