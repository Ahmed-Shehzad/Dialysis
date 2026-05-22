using System.Runtime.CompilerServices;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionsByPatient;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PDMS.Tests;

public sealed class ListSessionsByPatientQueryHandlerTests
{
    [Fact]
    public async Task Returns_Patient_Sessions_Ordered_Most_Recent_First_Async()
    {
        var patient = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        // DialysisSession.Schedule rejects starts more than 1h in the past, so schedule
        // each session in a valid window then backdate ScheduledStartUtc via the
        // non-public setter (matches the pattern in PdmsDialysisSessionProcedureFeederTests).
        var older = NewSessionAt(patient, nowUtc.AddDays(-30));
        var newer = NewSessionAt(patient, nowUtc.AddDays(-2));
        var someoneElse = NewSessionAt(Guid.NewGuid(), nowUtc.AddDays(-1));

        var handler = new ListSessionsByPatientQueryHandler(
            new InMemorySessions(older, newer, someoneElse),
            new FakeTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new ListSessionsByPatientQuery(patient, LookbackDays: 90),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(newer.Id, "Most recent session is returned first.");
        result[1].Id.ShouldBe(older.Id);
        result.ShouldNotContain(r => r.Id == someoneElse.Id, "Other patients' sessions are filtered out.");
    }

    [Fact]
    public async Task Respects_Take_Clamp_Async()
    {
        var patient = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;
        var first = NewSessionAt(patient, nowUtc.AddDays(-1));
        var second = NewSessionAt(patient, nowUtc.AddDays(-2));
        var third = NewSessionAt(patient, nowUtc.AddDays(-3));

        var handler = new ListSessionsByPatientQueryHandler(
            new InMemorySessions(first, second, third),
            new FakeTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new ListSessionsByPatientQuery(patient, LookbackDays: 90, Take: 2),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(first.Id);
        result[1].Id.ShouldBe(second.Id);
    }

    private static DialysisSession NewSessionAt(Guid patientId, DateTime backdatedStart)
    {
        // Schedule in a valid window (1 minute in the future) then backdate the
        // ScheduledStartUtc property via its non-public setter.
        var session = DialysisSession.Schedule(
            id: Guid.NewGuid(),
            patientId: patientId,
            scheduledStartUtc: DateTime.UtcNow.AddMinutes(1),
            prescription: new SessionPrescription(
                dialyzerModel: "Polyflux 17L",
                prescribedDurationMinutes: 240,
                bloodFlowRateMlPerMin: 350,
                dialysateFlowRateMlPerMin: 500,
                dialysatePotassiumMmolPerL: 2.0m,
                dialysateCalciumMmolPerL: 1.25m,
                dialysateSodiumMmolPerL: 140m,
                targetUfVolumeLiters: 2.5m,
                anticoagulationProtocolCode: "heparin-bolus"),
            access: new VascularAccess(
                VascularAccessKind.ArteriovenousFistula,
                "Left forearm",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1))));

        var prop = typeof(DialysisSession).GetProperty(nameof(DialysisSession.ScheduledStartUtc))
            ?? throw new InvalidOperationException("ScheduledStartUtc not found");
        prop.GetSetMethod(nonPublic: true)!.Invoke(session, [backdatedStart]);
        return session;
    }

    private sealed class InMemorySessions(params DialysisSession[] sessions) : IDialysisSessionRepository
    {
        public Task<DialysisSession?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(sessions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<DialysisSession>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DialysisSession>>(
                [.. sessions.Where(s => s.PatientId == patientId && s.ScheduledStartUtc >= sinceUtc)]);

        public Task<IReadOnlyList<DialysisSession>> ListRecentAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DialysisSession>>([]);

        public Task<IReadOnlyList<DialysisSession>> ListActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DialysisSession>>([]);

        public void Add(DialysisSession session) => throw new NotSupportedException();

        public async IAsyncEnumerable<DialysisSession> StreamAllAsync(
            DateTimeOffset? since,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var session in sessions)
            {
                yield return session;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTime _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
