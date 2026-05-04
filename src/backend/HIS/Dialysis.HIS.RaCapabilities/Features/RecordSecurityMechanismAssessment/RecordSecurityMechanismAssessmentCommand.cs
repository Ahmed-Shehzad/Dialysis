using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;

public sealed record RecordSecurityMechanismAssessmentCommand(string MechanismCode, string AppliedLevel, string Notes)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
