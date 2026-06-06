using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.ClinicalNotes;

public sealed class CohortQualityTests
{
    private static PatientSummary Patient(Guid id, string mrn) =>
        new(id, mrn, "Doe", "Jane", new DateOnly(1970, 1, 1), "F", "Active");

    [Fact]
    public async Task Aggregates_Open_Gaps_Across_The_Cohort_Async()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var cohort = new PatientSearchResult([Patient(p1, "MRN-1"), Patient(p2, "MRN-2"), Patient(p3, "MRN-3")], 3, 0, 100);

        // p1 + p2 both miss the HbA1c measure; p2 also misses a lipid measure; p3 is clean.
        var evaluator = new FakeEvaluator(new Dictionary<Guid, IReadOnlyList<QualityGap>>
        {
            [p1] = [new QualityGap("MIPS-001", "HbA1c", "missing")],
            [p2] = [new QualityGap("MIPS-001", "HbA1c", "missing"), new QualityGap("MIPS-002", "Lipid", "missing")],
            [p3] = [],
        });
        var handler = new EvaluateCohortQualityQueryHandler(new FakeGateway(cohort), evaluator);

        var result = await handler.HandleAsync(new EvaluateCohortQualityQuery(), CancellationToken.None);

        result.PatientsEvaluated.ShouldBe(3);
        result.PatientsWithAnyGap.ShouldBe(2);
        result.PatientBreakdown.Count.ShouldBe(2);
        // Most-missed measure first.
        result.MeasureGaps[0].MeasureId.ShouldBe("MIPS-001");
        result.MeasureGaps[0].PatientsWithGap.ShouldBe(2);
        result.MeasureGaps.Single(m => m.MeasureId == "MIPS-002").PatientsWithGap.ShouldBe(1);
    }

    [Fact]
    public async Task No_Gaps_When_Every_Patient_Is_Clean_Async()
    {
        var p1 = Guid.NewGuid();
        var cohort = new PatientSearchResult([Patient(p1, "MRN-1")], 1, 0, 100);
        var handler = new EvaluateCohortQualityQueryHandler(
            new FakeGateway(cohort),
            new FakeEvaluator(new Dictionary<Guid, IReadOnlyList<QualityGap>> { [p1] = [] }));

        var result = await handler.HandleAsync(new EvaluateCohortQualityQuery(), CancellationToken.None);

        result.PatientsEvaluated.ShouldBe(1);
        result.PatientsWithAnyGap.ShouldBe(0);
        result.MeasureGaps.ShouldBeEmpty();
        result.PatientBreakdown.ShouldBeEmpty();
    }

    private sealed class FakeEvaluator : IQualityMeasureEvaluator
    {
        private readonly IReadOnlyDictionary<Guid, IReadOnlyList<QualityGap>> _gaps;
        public FakeEvaluator(IReadOnlyDictionary<Guid, IReadOnlyList<QualityGap>> gaps) => _gaps = gaps;
        public Task<IReadOnlyList<QualityGap>> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_gaps.TryGetValue(patientId, out var g) ? g : []);
    }

    private sealed class FakeGateway : ICqrsGateway
    {
        private readonly PatientSearchResult _cohort;
        public FakeGateway(PatientSearchResult cohort) => _cohort = cohort;

        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResponse> =>
            Task.FromResult((TResponse)(object)_cohort);

        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse> => throw new NotSupportedException();
    }
}
