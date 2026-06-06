using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.CarePlanning;

public sealed class CreateCarePlanCommandHandler : ICommandHandler<CreateCarePlanCommand, Guid>
{
    private readonly ICarePlanRepository _carePlans;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public CreateCarePlanCommandHandler(ICarePlanRepository carePlans, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _carePlans = carePlans;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(CreateCarePlanCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var plan = CarePlan.Create(id, request.PatientId, request.Title, request.AuthoredByProviderId,
            _timeProvider.GetUtcNow().UtcDateTime);
        _carePlans.Add(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}

public sealed class AddCarePlanGoalCommandHandler : ICommandHandler<AddCarePlanGoalCommand, Guid>
{
    private readonly ICarePlanRepository _carePlans;
    private readonly IUnitOfWork _unitOfWork;
    public AddCarePlanGoalCommandHandler(ICarePlanRepository carePlans, IUnitOfWork unitOfWork)
    {
        _carePlans = carePlans;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(AddCarePlanGoalCommand request, CancellationToken cancellationToken)
    {
        var plan = await _carePlans.GetAsync(request.CarePlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Care plan not found.");
        var goalId = Guid.CreateVersion7();
        plan.AddGoal(goalId, request.Description, request.TargetMeasure);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return goalId;
    }
}

public sealed class UpdateCarePlanGoalStatusCommandHandler : ICommandHandler<UpdateCarePlanGoalStatusCommand, Guid>
{
    private readonly ICarePlanRepository _carePlans;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateCarePlanGoalStatusCommandHandler(ICarePlanRepository carePlans, IUnitOfWork unitOfWork)
    {
        _carePlans = carePlans;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(UpdateCarePlanGoalStatusCommand request, CancellationToken cancellationToken)
    {
        var plan = await _carePlans.GetAsync(request.CarePlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Care plan not found.");
        plan.UpdateGoalStatus(request.GoalId, request.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return request.GoalId;
    }
}

public sealed class CloseCarePlanCommandHandler : ICommandHandler<CloseCarePlanCommand, Guid>
{
    private readonly ICarePlanRepository _carePlans;
    private readonly IUnitOfWork _unitOfWork;
    public CloseCarePlanCommandHandler(ICarePlanRepository carePlans, IUnitOfWork unitOfWork)
    {
        _carePlans = carePlans;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(CloseCarePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _carePlans.GetAsync(request.CarePlanId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Care plan not found.");
        plan.Close(request.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plan.Id;
    }
}
