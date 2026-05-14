using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;

public sealed record RegisterFinancialErpLinkCommand(string SystemCode, string StatusCode)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
