using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;

public sealed record RecordClinicalDecisionSupportEvaluationCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordClinicalDecisionSupportEvaluationCommand(Guid PatientId,
        string ChecksAppliedJson,
        bool SafeToProceed)
    {
        this.PatientId = PatientId;
        this.ChecksAppliedJson = ChecksAppliedJson;
        this.SafeToProceed = SafeToProceed;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid PatientId { get; init; }
    public string ChecksAppliedJson { get; init; }
    public bool SafeToProceed { get; init; }
    public void Deconstruct(out Guid PatientId, out string ChecksAppliedJson, out bool SafeToProceed)
    {
        PatientId = this.PatientId;
        ChecksAppliedJson = this.ChecksAppliedJson;
        SafeToProceed = this.SafeToProceed;
    }
}
