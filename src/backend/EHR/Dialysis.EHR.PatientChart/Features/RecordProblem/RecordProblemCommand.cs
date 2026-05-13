using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordProblem;

public sealed record RecordProblemCommand(
    Guid PatientId,
    string ConditionSystem,
    string ConditionCode,
    string? ConditionDisplay,
    DateOnly OnsetDate,
    string? Notes)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ProblemRecord;
}
