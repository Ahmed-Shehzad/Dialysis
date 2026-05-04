using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterResearchEducationActivity;

public sealed record RegisterResearchEducationActivityCommand(
    string ActivityKindCode,
    string Title,
    string ExternalReference,
    DateTime? RecordedAtUtc = null)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
