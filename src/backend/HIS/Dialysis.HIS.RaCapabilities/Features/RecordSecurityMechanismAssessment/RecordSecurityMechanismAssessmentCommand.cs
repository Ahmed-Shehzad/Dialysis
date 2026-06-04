using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;

public sealed record RecordSecurityMechanismAssessmentCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordSecurityMechanismAssessmentCommand(string MechanismCode, string AppliedLevel, string Notes)
    {
        this.MechanismCode = MechanismCode;
        this.AppliedLevel = AppliedLevel;
        this.Notes = Notes;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public string MechanismCode { get; init; }
    public string AppliedLevel { get; init; }
    public string Notes { get; init; }
    public void Deconstruct(out string MechanismCode, out string AppliedLevel, out string Notes)
    {
        MechanismCode = this.MechanismCode;
        AppliedLevel = this.AppliedLevel;
        Notes = this.Notes;
    }
}
