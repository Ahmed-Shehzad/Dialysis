using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientChart.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.CarePlanning;

/// <summary>Returns the patient's active care plan (with goals), or null when there isn't one.</summary>
public sealed record GetActiveCarePlanQuery(Guid PatientId)
    : IQuery<CarePlanView?>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CarePlanRead;
}

/// <summary>A care plan projected for the chart / portal.</summary>
public sealed record CarePlanView(
    Guid Id,
    Guid PatientId,
    string Title,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<CarePlanGoalView> Goals);

/// <summary>A goal projected for the chart / portal.</summary>
public sealed record CarePlanGoalView(Guid Id, string Description, string? TargetMeasure, string Status);

public sealed class GetActiveCarePlanQueryHandler : IQueryHandler<GetActiveCarePlanQuery, CarePlanView?>
{
    private readonly ICarePlanRepository _carePlans;
    public GetActiveCarePlanQueryHandler(ICarePlanRepository carePlans) => _carePlans = carePlans;

    public async Task<CarePlanView?> HandleAsync(GetActiveCarePlanQuery request, CancellationToken cancellationToken)
    {
        var plan = await _carePlans.GetActiveByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
            return null;
        return new CarePlanView(
            plan.Id, plan.PatientId, plan.Title, plan.Status.ToString(), plan.CreatedAtUtc,
            [.. plan.Goals.Select(g => new CarePlanGoalView(g.Id, g.Description, g.TargetMeasure, g.Status.ToString()))]);
    }
}
