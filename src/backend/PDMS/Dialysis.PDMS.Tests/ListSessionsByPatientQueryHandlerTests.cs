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

        var older = NewSession(patient, nowUtc.AddDays(-30));
        var newer = NewSession(patient, nowUtc.AddDays(-2));
        var someoneElse = NewSession(Guid.NewGuid(), nowUtc.AddDays(-1));

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
        var first = NewSession(patient, nowUtc.AddDays(-1));
        var second = NewSession(patient, nowUtc.AddDays(-2));
        var third = NewSession(patient, nowUtc.AddDays(-3));

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

    private static DialysisSession NewSession(Guid patientId, DateTime scheduledStart) =>
        DialysisSession.Schedule(
            id: Guid.NewGuid(),
            patientId: patientId,
            scheduledStartUtc: scheduledStart,
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
