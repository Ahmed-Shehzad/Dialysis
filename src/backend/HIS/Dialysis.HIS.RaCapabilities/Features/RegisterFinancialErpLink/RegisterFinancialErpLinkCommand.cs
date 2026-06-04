using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;

public sealed record RegisterFinancialErpLinkCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterFinancialErpLinkCommand(string SystemCode, string StatusCode)
    {
        this.SystemCode = SystemCode;
        this.StatusCode = StatusCode;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public string SystemCode { get; init; }
    public string StatusCode { get; init; }
    public void Deconstruct(out string SystemCode, out string StatusCode)
    {
        SystemCode = this.SystemCode;
        StatusCode = this.StatusCode;
    }
}
