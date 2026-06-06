using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class QualityMeasureEvaluatorTests
{
    private static readonly DateTime _nowUtc = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    private static QualityMeasureEvaluator Evaluator(
        QualityMeasureOptions options, IReadOnlyList<ProblemListItem> problems, IReadOnlyList<LabResult> labs) =>
        new(new StubProblems(problems), new StubLabs(labs), new FixedClock(_nowUtc), Options.Create(options));

    private static ProblemListItem Problem(Guid patientId, string icd10) =>
        ProblemListItem.Record(Guid.NewGuid(), patientId, new Coding("http://hl7.org/fhir/sid/icd-10-cm", icd10, icd10), new DateOnly(2025, 1, 1));

    private static LabResult Lab(Guid patientId, string loinc, DateTime observedAtUtc) =>
        LabResult.Receive(Guid.NewGuid(), Guid.NewGuid(), patientId, loinc, "6.5", LabAbnormalFlag.Normal, observedAtUtc);

    [Fact]
    public async Task Empty_Measures_Raise_No_Gaps_Async()
    {
        var patient = Guid.NewGuid();
        var gaps = await Evaluator(new QualityMeasureOptions(), [Problem(patient, "E11.9")], [])
            .EvaluateAsync(patient, CancellationToken.None);
        gaps.ShouldBeEmpty();
    }

    [Fact]
    public async Task Applicable_Measure_With_No_Recent_Result_Raises_A_Gap_Async()
    {
        var patient = Guid.NewGuid();
        var options = new QualityMeasureOptions
        {
            Measures =
            {
                new QualityMeasureRule
                {
                    Id = "MIPS-001", Title = "Diabetes: HbA1c poor control",
                    AppliesToAnyIcd10 = { "E11.9" }, ExpectedLoinc = "4548-4", WithinMonths = 12,
                },
            },
        };

        var gaps = await Evaluator(options, [Problem(patient, "E11.9")], []).EvaluateAsync(patient, CancellationToken.None);

        gaps.ShouldHaveSingleItem().MeasureId.ShouldBe("MIPS-001");
    }

    [Fact]
    public async Task Measure_Is_Satisfied_By_A_Recent_Result_Async()
    {
        var patient = Guid.NewGuid();
        var options = new QualityMeasureOptions
        {
            Measures =
            {
                new QualityMeasureRule { Id = "MIPS-001", Title = "HbA1c", AppliesToAnyIcd10 = { "E11.9" }, ExpectedLoinc = "4548-4", WithinMonths = 12 },
            },
        };

        var gaps = await Evaluator(options, [Problem(patient, "E11.9")], [Lab(patient, "4548-4", _nowUtc.AddMonths(-3))])
            .EvaluateAsync(patient, CancellationToken.None);

        gaps.ShouldBeEmpty();
    }

    [Fact]
    public async Task Measure_Does_Not_Apply_Without_The_Triggering_Problem_Async()
    {
        var patient = Guid.NewGuid();
        var options = new QualityMeasureOptions
        {
            Measures =
            {
                new QualityMeasureRule { Id = "MIPS-001", Title = "HbA1c", AppliesToAnyIcd10 = { "E11.9" }, ExpectedLoinc = "4548-4", WithinMonths = 12 },
            },
        };

        // Patient has a different problem → measure doesn't apply → no gap.
        var gaps = await Evaluator(options, [Problem(patient, "N18.6")], []).EvaluateAsync(patient, CancellationToken.None);

        gaps.ShouldBeEmpty();
    }

    private sealed class StubProblems : IProblemListRepository
    {
        private readonly IReadOnlyList<ProblemListItem> _items;
        public StubProblems(IReadOnlyList<ProblemListItem> items) => _items = items;
        public Task<IReadOnlyList<ProblemListItem>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProblemListItem>>([.. _items.Where(p => p.PatientId == patientId)]);
        public Task<ProblemListItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ProblemListItem?>(null);
        public void Add(ProblemListItem item) { }
    }

    private sealed class StubLabs : ILabResultRepository
    {
        private readonly IReadOnlyList<LabResult> _items;
        public StubLabs(IReadOnlyList<LabResult> items) => _items = items;
        public Task<IReadOnlyList<LabResult>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LabResult>>([.. _items.Where(l => l.PatientId == patientId && l.ObservedAtUtc >= sinceUtc)]);
        public Task<IReadOnlyList<LabResult>> ListByOrderAsync(Guid labOrderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LabResult>>([]);
        public void Add(LabResult result) { }
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
