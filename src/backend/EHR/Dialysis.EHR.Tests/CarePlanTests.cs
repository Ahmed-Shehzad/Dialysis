using System.Runtime.CompilerServices;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Features.CarePlanning;
using Dialysis.EHR.PatientChart.Ports;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class CarePlanTests
{
    private static readonly DateTime _nowUtc = new(2026, 6, 6, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Add_Goal_Update_Status_And_Close_Lifecycle()
    {
        var plan = CarePlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Dialysis adequacy", Guid.NewGuid(), _nowUtc);
        plan.Status.ShouldBe(CarePlanStatus.Active);

        var goal = plan.AddGoal(Guid.NewGuid(), "Achieve Kt/V ≥ 1.4", "Kt/V");
        goal.Status.ShouldBe(CarePlanGoalStatus.Proposed);
        plan.Goals.ShouldHaveSingleItem();

        plan.UpdateGoalStatus(goal.Id, CarePlanGoalStatus.InProgress);
        plan.Goals.Single().Status.ShouldBe(CarePlanGoalStatus.InProgress);

        plan.Close(CarePlanStatus.Completed);
        plan.Status.ShouldBe(CarePlanStatus.Completed);
    }

    [Fact]
    public void Cannot_Add_A_Goal_To_A_Closed_Plan()
    {
        var plan = CarePlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan", Guid.NewGuid(), _nowUtc);
        plan.Close(CarePlanStatus.Revoked);
        Should.Throw<InvalidOperationException>(() => plan.AddGoal(Guid.NewGuid(), "Late goal", null));
    }

    [Fact]
    public void Close_Requires_Completed_Or_Revoked()
    {
        var plan = CarePlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan", Guid.NewGuid(), _nowUtc);
        Should.Throw<ArgumentException>(() => plan.Close(CarePlanStatus.Active));
    }

    [Fact]
    public async Task Add_Goal_Handler_Persists_To_The_Loaded_Plan_Async()
    {
        var plan = CarePlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan", Guid.NewGuid(), _nowUtc);
        var repo = new InMemoryCarePlanRepository(plan);
        var handler = new AddCarePlanGoalCommandHandler(repo, new NoopUnitOfWork());

        var goalId = await handler.HandleAsync(
            new AddCarePlanGoalCommand(plan.Id, "Reduce interdialytic weight gain", "kg"), CancellationToken.None);

        plan.Goals.ShouldHaveSingleItem().Id.ShouldBe(goalId);
    }

    private sealed class InMemoryCarePlanRepository : ICarePlanRepository
    {
        private readonly CarePlan _plan;
        public InMemoryCarePlanRepository(CarePlan plan) => _plan = plan;
        public Task<CarePlan?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CarePlan?>(_plan.Id == id ? _plan : null);
        public Task<CarePlan?> GetActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CarePlan?>(_plan.PatientId == patientId ? _plan : null);
        public void Add(CarePlan carePlan) { }
        public async IAsyncEnumerable<CarePlan> StreamAllAsync(
            DateTimeOffset? since,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = since;
            yield return _plan;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
