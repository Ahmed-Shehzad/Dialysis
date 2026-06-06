using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.ClinicalDecisionSupport;

public sealed class ClinicalDecisionSupportEvaluatorTests
{
    private static readonly DateTime _nowUtc = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);
    private const string Icd10System = "http://hl7.org/fhir/sid/icd-10-cm";
    private const string LoincSystem = "http://loinc.org";

    private static ClinicalDecisionSupportEvaluator Evaluator(
        CdsOptions options,
        IReadOnlyList<ProblemListItem>? problems = null,
        IReadOnlyList<MedicationStatement>? meds = null,
        IReadOnlyList<VitalSignReading>? vitals = null,
        IReadOnlyList<LabResult>? labs = null) =>
        new(new StubProblems(problems ?? []), new StubMeds(meds ?? []), new StubVitals(vitals ?? []),
            new StubLabs(labs ?? []), new FixedClock(_nowUtc), Options.Create(options));

    private static ProblemListItem Problem(Guid patientId, string icd10) =>
        ProblemListItem.Record(Guid.NewGuid(), patientId, new Coding(Icd10System, icd10, icd10), new DateOnly(2025, 1, 1));

    private static CdsOptions With(CdsRule rule) => new() { Rules = { rule } };

    [Fact]
    public async Task Empty_Config_Raises_Nothing_Async()
    {
        var patient = Guid.NewGuid();
        var recs = await Evaluator(new CdsOptions(), [Problem(patient, "J45.909")]).EvaluateAsync(patient, CancellationToken.None);
        recs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Missing_Lab_Trigger_Fires_When_No_Recent_Result_Async()
    {
        var patient = Guid.NewGuid();
        var options = With(new CdsRule
        {
            Id = "ASTHMA-SPIRO", Title = "Asthma: order spirometry", AppliesToAnyIcd10 = { "J45.909" },
            TriggerKind = CdsTriggerKind.MissingLabWithinMonths, ExpectedLoinc = "19868-9", WithinMonths = 12,
        });

        var recs = await Evaluator(options, [Problem(patient, "J45.909")]).EvaluateAsync(patient, CancellationToken.None);

        recs.ShouldHaveSingleItem().RuleId.ShouldBe("ASTHMA-SPIRO");
    }

    [Fact]
    public async Task Abnormal_Vital_Trigger_Fires_When_Latest_Reading_Exceeds_Target_Async()
    {
        var patient = Guid.NewGuid();
        var options = With(new CdsRule
        {
            Id = "HTN-BP", Title = "Hypertension: BP above target", AppliesToAnyIcd10 = { "I10" },
            TriggerKind = CdsTriggerKind.AbnormalVitalThreshold, VitalLoinc = "8480-6",
            Comparator = ClinicalComparator.GreaterThan, ThresholdValue = 140m, VitalWithinDays = 180,
        });
        var bp = VitalSignReading.Record(Guid.NewGuid(), patient, new Coding(LoincSystem, "8480-6", "Systolic BP"),
            152m, "mm[Hg]", _nowUtc.AddDays(-3));

        var recs = await Evaluator(options, [Problem(patient, "I10")], vitals: [bp]).EvaluateAsync(patient, CancellationToken.None);

        recs.ShouldHaveSingleItem().RuleId.ShouldBe("HTN-BP");
    }

    [Fact]
    public async Task Abnormal_Vital_Trigger_Quiet_When_Controlled_Async()
    {
        var patient = Guid.NewGuid();
        var options = With(new CdsRule
        {
            Id = "HTN-BP", Title = "Hypertension: BP above target", AppliesToAnyIcd10 = { "I10" },
            TriggerKind = CdsTriggerKind.AbnormalVitalThreshold, VitalLoinc = "8480-6",
            Comparator = ClinicalComparator.GreaterThan, ThresholdValue = 140m,
        });
        var bp = VitalSignReading.Record(Guid.NewGuid(), patient, new Coding(LoincSystem, "8480-6", "Systolic BP"),
            128m, "mm[Hg]", _nowUtc.AddDays(-3));

        var recs = await Evaluator(options, [Problem(patient, "I10")], vitals: [bp]).EvaluateAsync(patient, CancellationToken.None);

        recs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Condition_Without_Medication_Class_Fires_When_No_Controller_Async()
    {
        var patient = Guid.NewGuid();
        var options = With(new CdsRule
        {
            Id = "ASTHMA-CTRL", Title = "Asthma: ensure controller medication", AppliesToAnyIcd10 = { "J45.909" },
            TriggerKind = CdsTriggerKind.ConditionWithoutMedicationClass, MedicationCodePrefixAny = { "INH-CORT" },
        });
        var reliever = MedicationStatement.Record(Guid.NewGuid(), patient,
            new Coding("http://www.nlm.nih.gov/research/umls/rxnorm", "ALBUTEROL", "Albuterol"), "2 puffs", "PRN", new DateOnly(2026, 1, 1));

        var recs = await Evaluator(options, [Problem(patient, "J45.909")], meds: [reliever]).EvaluateAsync(patient, CancellationToken.None);

        recs.ShouldHaveSingleItem().RuleId.ShouldBe("ASTHMA-CTRL");
    }

    [Fact]
    public async Task Rule_Does_Not_Apply_Without_The_Triggering_Condition_Async()
    {
        var patient = Guid.NewGuid();
        var options = With(new CdsRule
        {
            Id = "HTN-BP", Title = "BP above target", AppliesToAnyIcd10 = { "I10" },
            TriggerKind = CdsTriggerKind.AbnormalVitalThreshold, VitalLoinc = "8480-6",
            Comparator = ClinicalComparator.GreaterThan, ThresholdValue = 140m,
        });
        var bp = VitalSignReading.Record(Guid.NewGuid(), patient, new Coding(LoincSystem, "8480-6"), 160m, "mm[Hg]", _nowUtc.AddDays(-1));

        // Patient has a different problem → the I10 rule doesn't apply even though BP is high.
        var recs = await Evaluator(options, [Problem(patient, "N18.6")], vitals: [bp]).EvaluateAsync(patient, CancellationToken.None);

        recs.ShouldBeEmpty();
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

    private sealed class StubMeds : IMedicationStatementRepository
    {
        private readonly IReadOnlyList<MedicationStatement> _items;
        public StubMeds(IReadOnlyList<MedicationStatement> items) => _items = items;
        public Task<MedicationStatement?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<MedicationStatement?>(null);
        public Task<IReadOnlyList<MedicationStatement>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MedicationStatement>>([.. _items.Where(m => m.PatientId == patientId)]);
        public void Add(MedicationStatement statement) { }
        public IAsyncEnumerable<MedicationStatement> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
