using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.ListLabResultsForPatient;
using Dialysis.EHR.ClinicalNotes.Ports;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class ListLabResultsForPatientQueryHandlerTests
{
    [Fact]
    public async Task Returns_Results_Ordered_Most_Recent_First_And_Maps_Flags_Async()
    {
        var patient = Guid.NewGuid();
        var order = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var older = LabResult.Receive(
            id: Guid.NewGuid(),
            labOrderId: order,
            patientId: patient,
            loincCode: "2160-0",
            valueText: "1.2",
            abnormalFlag: LabAbnormalFlag.Normal,
            observedAtUtc: nowUtc.AddDays(-30),
            unitCode: "mg/dL");

        var newer = LabResult.Receive(
            id: Guid.NewGuid(),
            labOrderId: order,
            patientId: patient,
            loincCode: "2823-3",
            valueText: "5.8",
            abnormalFlag: LabAbnormalFlag.High,
            observedAtUtc: nowUtc.AddDays(-1),
            unitCode: "mmol/L");

        var handler = new ListLabResultsForPatientQueryHandler(
            new InMemoryResults(older, newer),
            new FakeTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new ListLabResultsForPatientQuery(patient, LookbackDays: 90),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(newer.Id, "Most recent result first.");
        result[0].AbnormalFlag.ShouldBe((int)LabAbnormalFlag.High);
        result[1].Id.ShouldBe(older.Id);
        result[1].AbnormalFlag.ShouldBe((int)LabAbnormalFlag.Normal);
    }

    [Fact]
    public async Task Honours_Lookback_Window_Async()
    {
        var patient = Guid.NewGuid();
        var order = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var within = LabResult.Receive(
            id: Guid.NewGuid(),
            labOrderId: order,
            patientId: patient,
            loincCode: "718-7",
            valueText: "12.5",
            abnormalFlag: LabAbnormalFlag.Normal,
            observedAtUtc: nowUtc.AddDays(-5));

        var outside = LabResult.Receive(
            id: Guid.NewGuid(),
            labOrderId: order,
            patientId: patient,
            loincCode: "718-7",
            valueText: "13.5",
            abnormalFlag: LabAbnormalFlag.Normal,
            observedAtUtc: nowUtc.AddDays(-200));

        var handler = new ListLabResultsForPatientQueryHandler(
            new InMemoryResults(within, outside),
            new FakeTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new ListLabResultsForPatientQuery(patient, LookbackDays: 30),
            CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(within.Id);
    }

    private sealed class InMemoryResults(params LabResult[] seed) : ILabResultRepository
    {
        private readonly IReadOnlyList<LabResult> _results = [.. seed];

        public Task<IReadOnlyList<LabResult>> ListByOrderAsync(Guid labOrderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LabResult>>([.. _results.Where(r => r.LabOrderId == labOrderId)]);

        public Task<IReadOnlyList<LabResult>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LabResult>>(
                [.. _results.Where(r => r.PatientId == patientId && r.ObservedAtUtc >= sinceUtc)]);

        public void Add(LabResult result) => throw new NotSupportedException();
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTime _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
