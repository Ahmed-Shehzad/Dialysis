using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;

public sealed record PostOrganizationalCommunicationCommand : ICommand<Guid>, IPermissionedCommand
{
    public PostOrganizationalCommunicationCommand(string ThreadCode, string Subject, string Body)
    {
        this.ThreadCode = ThreadCode;
        this.Subject = Subject;
        this.Body = Body;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public string ThreadCode { get; init; }
    public string Subject { get; init; }
    public string Body { get; init; }
    public void Deconstruct(out string ThreadCode, out string Subject, out string Body)
    {
        ThreadCode = this.ThreadCode;
        Subject = this.Subject;
        Body = this.Body;
    }
}
