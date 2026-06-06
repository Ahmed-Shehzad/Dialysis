using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.CarePlanning;

/// <summary>Creates an active care plan for a patient. Returns the new plan id.</summary>
public sealed record CreateCarePlanCommand(Guid PatientId, string Title, Guid AuthoredByProviderId)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CarePlanWrite;
}

/// <summary>Adds a goal to a care plan. Returns the new goal id.</summary>
public sealed record AddCarePlanGoalCommand(Guid CarePlanId, string Description, string? TargetMeasure)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CarePlanWrite;
}

/// <summary>Updates a goal's status. Returns the goal id.</summary>
public sealed record UpdateCarePlanGoalStatusCommand(Guid CarePlanId, Guid GoalId, CarePlanGoalStatus Status)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CarePlanWrite;
}

/// <summary>Closes a care plan as Completed or Revoked. Returns the plan id.</summary>
public sealed record CloseCarePlanCommand(Guid CarePlanId, CarePlanStatus Status)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CarePlanWrite;
}
