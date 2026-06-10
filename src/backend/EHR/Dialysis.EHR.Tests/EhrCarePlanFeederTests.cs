using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Fhir;
using Dialysis.EHR.PatientChart.Ports;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using CarePlan = Dialysis.EHR.PatientChart.Domain.CarePlan;
using FhirCarePlan = Hl7.Fhir.Model.CarePlan;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class EhrCarePlanFeederTests
{
    [Fact]
    public async Task Streamasync_Projects_Plan_With_Goals_As_Activities_Async()
    {
        var patient = Guid.NewGuid();
        var plan = CarePlan.Create(Guid.NewGuid(), patient, "Dialysis adequacy", Guid.NewGuid(),
            new DateTime(2026, 6, 6, 9, 0, 0, DateTimeKind.Utc));
        var goal = plan.AddGoal(Guid.NewGuid(), "Achieve Kt/V ≥ 1.4", "Kt/V");
        plan.UpdateGoalStatus(goal.Id, CarePlanGoalStatus.InProgress);

        var feeder = new EhrCarePlanFeeder(new InMemoryCarePlans(plan));

        var results = new List<FhirCarePlan>();
        await foreach (var resource in feeder.StreamAsync(NewJob(), CancellationToken.None))
        {
            results.Add(resource);
        }

        var fhir = results.ShouldHaveSingleItem();
        fhir.Id.ShouldBe(plan.Id.ToString());
        fhir.Status.ShouldBe(RequestStatus.Active);
        fhir.Intent.ShouldBe(FhirCarePlan.CarePlanIntent.Plan);
        fhir.Title.ShouldBe("Dialysis adequacy");
        fhir.Subject.Reference.ShouldBe($"Patient/{patient}");

        var detail = fhir.Activity.ShouldHaveSingleItem().Detail.ShouldNotBeNull();
        detail.Description.ShouldBe("Achieve Kt/V ≥ 1.4");
        detail.Status.ShouldBe(FhirCarePlan.CarePlanActivityStatus.InProgress);
        detail.Code.ShouldNotBeNull().Text.ShouldBe("Kt/V");
    }

    private static ExportJob NewJob() => new(
        Id: Guid.NewGuid().ToString("N"),
        Scope: ExportScope.System,
        GroupId: null,
        ResourceTypes: ["CarePlan"],
        Since: null,
        DeIdentificationProfile: null,
        RequestorId: null,
        Status: ExportJobStatus.InProgress,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null,
        Outputs: [],
        Error: null);

    private sealed class InMemoryCarePlans : ICarePlanRepository
    {
        private readonly CarePlan[] _plans;
        public InMemoryCarePlans(params CarePlan[] plans) => _plans = plans;
        public Task<CarePlan?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_plans.FirstOrDefault(p => p.Id == id));
        public Task<CarePlan?> GetActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_plans.FirstOrDefault(p => p.PatientId == patientId));
        public void Add(CarePlan carePlan) => throw new NotSupportedException();

        public async IAsyncEnumerable<CarePlan> StreamAllAsync(
            DateTimeOffset? since,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = since;
            foreach (var p in _plans)
            {
                yield return p;
                await Task.Yield();
            }
        }
    }
}
