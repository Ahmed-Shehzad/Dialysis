using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;

public sealed record RecordClinicalDecisionSupportEvaluationCommand(
    Guid PatientId,
    string ChecksAppliedJson,
    bool SafeToProceed) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
