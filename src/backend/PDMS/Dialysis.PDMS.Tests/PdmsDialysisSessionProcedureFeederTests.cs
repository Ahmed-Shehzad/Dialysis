using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Fhir;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Hl7.Fhir.Model;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PDMS.Tests;

public sealed class PdmsDialysisSessionProcedureFeederTests
{
    [Fact]
    public async Task Streamasync_Projects_Sessions_To_Procedure_With_Snomed_Code_And_Period_Async()
    {
        var scheduled = NewSession(scheduledStart: DateTime.UtcNow.AddHours(1));
        var inProgress = NewSession(scheduledStart: DateTime.UtcNow.AddMinutes(-30));
        inProgress.Start(DateTime.UtcNow.AddMinutes(-25));

        var feeder = new PdmsDialysisSessionProcedureFeeder(new InMemorySessions(scheduled, inProgress));

        var results = new List<Procedure>();
        await foreach (var procedure in feeder.StreamAsync(NewJob(since: null), CancellationToken.None))
        {
            results.Add(procedure);
        }

        results.Count.ShouldBe(2);
        results.ShouldAllBe(p => p.Code!.Coding[0].System == "http://snomed.info/sct" && p.Code!.Coding[0].Code == "302497006");

        var inProgressOut = results.First(p => p.Id == inProgress.Id.ToString());
        inProgressOut.Status.ShouldBe(EventStatus.InProgress);
        inProgressOut.Subject.Reference.ShouldBe($"Patient/{inProgress.PatientId}");

        var scheduledOut = results.First(p => p.Id == scheduled.Id.ToString());
        scheduledOut.Status.ShouldBe(EventStatus.Preparation);
    }

    [Fact]
    public async Task Streamasync_Passes_Since_Filter_Through_To_Repository_Async()
    {
        // Repository fake honours `since` by comparing on the session's latest start
        // timestamp. We assert the feeder forwards the job's Since value through.
        var keep = NewSession(scheduledStart: DateTime.UtcNow);
        var dropFake = NewSession(scheduledStart: DateTime.UtcNow.AddMinutes(30));
        SetForcedStaleTimestamp(dropFake, DateTime.UtcNow.AddDays(-30));

        var feeder = new PdmsDialysisSessionProcedureFeeder(new InMemorySessions(keep, dropFake));

        var results = new List<Procedure>();
        await foreach (var procedure in feeder.StreamAsync(NewJob(since: DateTimeOffset.UtcNow.AddDays(-7)), CancellationToken.None))
        {
            results.Add(procedure);
        }

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(keep.Id.ToString());
    }

    private static void SetForcedStaleTimestamp(DialysisSession session, DateTime staleUtc)
    {
        var prop = typeof(DialysisSession).GetProperty(nameof(DialysisSession.ScheduledStartUtc))
            ?? throw new InvalidOperationException("ScheduledStartUtc not found");
        prop.GetSetMethod(nonPublic: true)!.Invoke(session, [staleUtc]);
    }

    private static DialysisSession NewSession(DateTime scheduledStart) =>
        DialysisSession.Schedule(
            id: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
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

    private static ExportJob NewJob(DateTimeOffset? since) => new(
        Id: Guid.NewGuid().ToString("N"),
        Scope: ExportScope.System,
        GroupId: null,
        ResourceTypes: ["Procedure"],
        Since: since,
        DeIdentificationProfile: null,
        RequestorId: null,
        Status: ExportJobStatus.InProgress,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null,
        Outputs: Array.Empty<ExportJobOutput>(),
        Error: null);

    private sealed class InMemorySessions : IDialysisSessionRepository
    {
        private readonly DialysisSession[] _sessions;
        public InMemorySessions(params DialysisSession[] sessions) => _sessions = sessions;
        public Task<DialysisSession?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<DialysisSession>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DialysisSession>>([]);

        public Task<IReadOnlyList<DialysisSession>> ListRecentAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DialysisSession>>([]);

        public Task<IReadOnlyList<DialysisSession>> ListActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DialysisSession>>([]);

        public void Add(DialysisSession session) => throw new NotSupportedException();

        public async IAsyncEnumerable<DialysisSession> StreamAllAsync(
            DateTimeOffset? since,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var session in _sessions)
            {
                if (since is { } cutoff)
                {
                    var latestUtc = session.ActualStartUtc ?? session.ScheduledStartUtc;
                    if (latestUtc < cutoff.UtcDateTime) continue;
                }
                yield return session;
                await Task.Yield();
            }
        }
    }
}
