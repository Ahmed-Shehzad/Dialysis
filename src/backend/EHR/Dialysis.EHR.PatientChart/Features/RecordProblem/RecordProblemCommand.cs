using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordProblem;

public sealed record RecordProblemCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordProblemCommand(Guid PatientId,
        string ConditionSystem,
        string ConditionCode,
        string? ConditionDisplay,
        DateOnly OnsetDate,
        string? Notes)
    {
        this.PatientId = PatientId;
        this.ConditionSystem = ConditionSystem;
        this.ConditionCode = ConditionCode;
        this.ConditionDisplay = ConditionDisplay;
        this.OnsetDate = OnsetDate;
        this.Notes = Notes;
    }
    public string RequiredPermission => EhrPermissions.ProblemRecord;
    public Guid PatientId { get; init; }
    public string ConditionSystem { get; init; }
    public string ConditionCode { get; init; }
    public string? ConditionDisplay { get; init; }
    public DateOnly OnsetDate { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid PatientId, out string ConditionSystem, out string ConditionCode, out string? ConditionDisplay, out DateOnly OnsetDate, out string? Notes)
    {
        PatientId = this.PatientId;
        ConditionSystem = this.ConditionSystem;
        ConditionCode = this.ConditionCode;
        ConditionDisplay = this.ConditionDisplay;
        OnsetDate = this.OnsetDate;
        Notes = this.Notes;
    }
}
