using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Dialysis.EHR.Registration.Features.SearchPatients;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.ClinicalNotes;

public sealed class PopulationControlTests
{
    private static readonly DateTime _now = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);
    private const string LoincSystem = "http://loinc.org";
    private const string Icd10System = "http://hl7.org/fhir/sid/icd-10-cm";

    private static readonly ControlRule _bpRule = new()
    {
        Id = "HTN-BP",
        Title = "Hypertension: BP controlled",
        AppliesToAnyIcd10 = { "I10" },
        Kind = ControlKind.Vital,
        Code = "8480-6",
        Comparator = ClinicalComparator.LessThan,
        TargetValue = 140m,
        WithinDays = 180,
    };

    private static ProblemListItem Problem(Guid patientId, string icd10) =>
        ProblemListItem.Record(Guid.NewGuid(), patientId, new Coding(Icd10System, icd10, icd10), new DateOnly(2025, 1, 1));

    private static VitalSignReading Bp(Guid patientId, decimal systolic) =>
        VitalSignReading.Record(Guid.NewGuid(), patientId, new Coding(LoincSystem, "8480-6", "Systolic BP"), systolic, "mm[Hg]", _now.AddDays(-2));

    private static ConditionControlEvaluator Evaluator(
        IReadOnlyList<ProblemListItem> problems, IReadOnlyList<VitalSignReading> vitals) =>
        new(new StubProblems(problems), new StubVitals(vitals), new StubLabs([]), new FixedClock(_now));

    [Fact]
    public async Task Controlled_When_Latest_Reading_Meets_Target_Async()
    {
        var p = Guid.NewGuid();
        var status = await Evaluator([Problem(p, "I10")], [Bp(p, 128m)]).EvaluateAsync(p, _bpRule, CancellationToken.None);
        status.Outcome.ShouldBe(PatientControlOutcome.Controlled);
    }

    [Fact]
    public async Task Uncontrolled_When_Latest_Reading_Misses_Target_Async()
    {
        var p = Guid.NewGuid();
        var status = await Evaluator([Problem(p, "I10")], [Bp(p, 152m)]).EvaluateAsync(p, _bpRule, CancellationToken.None);
        status.Outcome.ShouldBe(PatientControlOutcome.Uncontrolled);
    }

    [Fact]
    public async Task No_Data_When_No_Reading_In_Window_Async()
    {
        var p = Guid.NewGuid();
        var status = await Evaluator([Problem(p, "I10")], []).EvaluateAsync(p, _bpRule, CancellationToken.None);
        status.Outcome.ShouldBe(PatientControlOutcome.NoData);
    }

    [Fact]
    public async Task Not_Applicable_Without_The_Condition_Async()
    {
        var p = Guid.NewGuid();
        var status = await Evaluator([Problem(p, "N18.6")], [Bp(p, 152m)]).EvaluateAsync(p, _bpRule, CancellationToken.None);
        status.Outcome.ShouldBe(PatientControlOutcome.NotApplicable);
    }

    [Fact]
    public async Task Handler_Aggregates_Control_Rate_Across_The_Cohort_Async()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var cohort = new PatientSearchResult(
            [Patient(p1, "MRN-1"), Patient(p2, "MRN-2"), Patient(p3, "MRN-3")], 3, 0, 100);
        var evaluator = new FakeControlEvaluator(new Dictionary<Guid, PatientControlOutcome>
        {
            [p1] = PatientControlOutcome.Controlled,
            [p2] = PatientControlOutcome.Uncontrolled,
            [p3] = PatientControlOutcome.NotApplicable, // dropped from the cohort
        });
        var options = Options.Create(new ControlMeasureOptions { Measures = { _bpRule } });
        var handler = new EvaluatePopulationControlQueryHandler(new FakeGateway(cohort), evaluator, options);

        var result = await handler.HandleAsync(new EvaluatePopulationControlQuery("HTN-BP"), CancellationToken.None);

        result.InCohort.ShouldBe(2);
        result.Controlled.ShouldBe(1);
        result.Uncontrolled.ShouldBe(1);
        result.ControlRatePercent.ShouldBe(50d);
    }

    private static PatientSummary Patient(Guid id, string mrn) =>
        new(id, mrn, "Doe", "Jane", new DateOnly(1960, 1, 1), "F", "Active");

    private sealed class FakeControlEvaluator : IConditionControlEvaluator
    {
        private readonly IReadOnlyDictionary<Guid, PatientControlOutcome> _outcomes;
        public FakeControlEvaluator(IReadOnlyDictionary<Guid, PatientControlOutcome> outcomes) => _outcomes = outcomes;
        public Task<PatientControlStatus> EvaluateAsync(Guid patientId, ControlRule rule, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PatientControlStatus(
                _outcomes.TryGetValue(patientId, out var o) ? o : PatientControlOutcome.NoData, null));
    }

    private sealed class FakeGateway : ICqrsGateway
    {
        private readonly PatientSearchResult _cohort;
        public FakeGateway(PatientSearchResult cohort) => _cohort = cohort;
        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResponse> => Task.FromResult((TResponse)(object)_cohort);
        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse> => throw new NotSupportedException();
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

    private sealed class StubVitals : IVitalSignRepository
    {
        private readonly IReadOnlyList<VitalSignReading> _items;
        public StubVitals(IReadOnlyList<VitalSignReading> items) => _items = items;
        public Task<IReadOnlyList<VitalSignReading>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<VitalSignReading>>([.. _items.Where(v => v.PatientId == patientId && v.ObservedAtUtc >= sinceUtc)]);
        public void Add(VitalSignReading reading) { }
        public IAsyncEnumerable<VitalSignReading> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
