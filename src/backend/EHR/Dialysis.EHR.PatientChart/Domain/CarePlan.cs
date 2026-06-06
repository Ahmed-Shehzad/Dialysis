using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

public enum CarePlanStatus
{
    Active = 1,
    Completed = 2,
    Revoked = 3,
}

public enum CarePlanGoalStatus
{
    Proposed = 1,
    InProgress = 2,
    Achieved = 3,
    NotAchieved = 4,
}

/// <summary>A trackable goal within a <see cref="CarePlan"/>.</summary>
public sealed class CarePlanGoal : Entity<Guid>
{
    private CarePlanGoal()
    {
    }

    internal CarePlanGoal(Guid id, Guid carePlanId, string description, string? targetMeasure) : base(id)
    {
        CarePlanId = carePlanId;
        Description = description;
        TargetMeasure = targetMeasure;
        Status = CarePlanGoalStatus.Proposed;
    }

    public Guid CarePlanId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string? TargetMeasure { get; private set; }

    public CarePlanGoalStatus Status { get; private set; }

    internal void SetStatus(CarePlanGoalStatus status) => Status = status;
}

/// <summary>
/// A structured, shareable plan of care for a patient with trackable goals — beyond the SOAP note's
/// free-text Plan. Lives in the PatientChart slice alongside the other longitudinal-record aggregates.
/// </summary>
public sealed class CarePlan : AggregateRoot<Guid>
{
    private readonly List<CarePlanGoal> _goals = new();

    private CarePlan()
    {
    }

    public CarePlan(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public CarePlanStatus Status { get; private set; }

    public Guid AuthoredByProviderId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CarePlanGoal> Goals => _goals;

    public static CarePlan Create(Guid id, Guid patientId, string title, Guid authoredByProviderId, DateTime nowUtc)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        if (authoredByProviderId == Guid.Empty) throw new ArgumentException("Authoring provider required.", nameof(authoredByProviderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new CarePlan(id)
        {
            PatientId = patientId,
            Title = title.Trim(),
            AuthoredByProviderId = authoredByProviderId,
            Status = CarePlanStatus.Active,
            CreatedAtUtc = nowUtc,
        };
    }

    public CarePlanGoal AddGoal(Guid goalId, string description, string? targetMeasure)
    {
        if (Status != CarePlanStatus.Active)
            throw new InvalidOperationException($"Cannot add a goal to a {Status} care plan.");
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var goal = new CarePlanGoal(
            goalId, Id, description.Trim(),
            string.IsNullOrWhiteSpace(targetMeasure) ? null : targetMeasure.Trim());
        _goals.Add(goal);
        return goal;
    }

    public void UpdateGoalStatus(Guid goalId, CarePlanGoalStatus status)
    {
        var goal = _goals.FirstOrDefault(g => g.Id == goalId)
            ?? throw new InvalidOperationException("Goal not found on this care plan.");
        goal.SetStatus(status);
    }

    public void Close(CarePlanStatus status)
    {
        if (status is not (CarePlanStatus.Completed or CarePlanStatus.Revoked))
            throw new ArgumentException("A care plan can only be closed as Completed or Revoked.", nameof(status));
        if (Status != CarePlanStatus.Active)
            throw new InvalidOperationException($"Care plan is already {Status}.");
        Status = status;
    }
}
