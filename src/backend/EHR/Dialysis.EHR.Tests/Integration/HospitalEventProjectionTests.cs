using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Integration.Consumers;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Dialysis.HIE.Contracts.Integration;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Integration;

/// <summary>
/// The care-coordination hospital-event read model: HIS admit/discharge and HIE external encounters are
/// projected into rows; external encounters are unmatched (null patient); the worklist + follow-up
/// semantics work; re-delivery is idempotent.
/// </summary>
public sealed class HospitalEventProjectionTests
{
    [Fact]
    public async Task Admission_Projects_A_Matched_Hospital_Event_Async()
    {
        var repo = new InMemoryHospitalEvents();
        var patient = Guid.NewGuid();
        await new PatientAdmittedHospitalEventProjector(repo, new NoopUnitOfWork())
            .HandleAsync(Ctx(new PatientAdmittedIntegrationEvent(
                Guid.CreateVersion7(), DateTime.UtcNow, 1, Guid.NewGuid(), patient, "4N", DateTime.UtcNow)));

        var row = repo.Rows.ShouldHaveSingleItem();
        row.Kind.ShouldBe(HospitalEventKind.Admitted);
        row.PatientId.ShouldBe(patient);
        row.Source.ShouldBe("HIS");
        row.FollowedUp.ShouldBeFalse();
    }

    [Fact]
    public async Task Discharge_Projects_A_Discharged_Event_Async()
    {
        var repo = new InMemoryHospitalEvents();
        await new PatientDischargedHospitalEventProjector(repo, new NoopUnitOfWork())
            .HandleAsync(Ctx(new PatientDischargedIntegrationEvent(
                Guid.CreateVersion7(), DateTime.UtcNow, 1, Guid.NewGuid(), Guid.NewGuid(), "4N", DateTime.UtcNow)));

        repo.Rows.ShouldHaveSingleItem().Kind.ShouldBe(HospitalEventKind.Discharged);
    }

    [Fact]
    public async Task External_Encounter_Projects_An_Unmatched_Event_With_The_External_Ref_Async()
    {
        var repo = new InMemoryHospitalEvents();
        await new ExternalEncounterHospitalEventProjector(repo, new NoopUnitOfWork())
            .HandleAsync(Ctx(new ExternalEncounterIngestedIntegrationEvent(
                Guid.CreateVersion7(), DateTime.UtcNow, 1, "partner-mercy", "enc-99", "ext-pat-7",
                DateTime.UtcNow, null, "IMP", "chest pain")));

        var row = repo.Rows.ShouldHaveSingleItem();
        row.Kind.ShouldBe(HospitalEventKind.ExternalEncounter);
        row.PatientId.ShouldBeNull();
        row.ExternalPatientRef.ShouldBe("ext-pat-7");
        row.Source.ShouldBe("partner-mercy");
    }

    [Fact]
    public async Task Redelivery_Is_Idempotent_On_Source_Key_Async()
    {
        var repo = new InMemoryHospitalEvents();
        var admissionId = Guid.NewGuid();
        var evt = new PatientAdmittedIntegrationEvent(
            Guid.CreateVersion7(), DateTime.UtcNow, 1, admissionId, Guid.NewGuid(), "4N", DateTime.UtcNow);
        var projector = new PatientAdmittedHospitalEventProjector(repo, new NoopUnitOfWork());

        await projector.HandleAsync(Ctx(evt));
        await projector.HandleAsync(Ctx(evt));

        repo.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Needs_Follow_Up_Excludes_Followed_Up_Rows_Async()
    {
        var repo = new InMemoryHospitalEvents();
        var followed = new HospitalEvent { Id = Guid.NewGuid(), Kind = HospitalEventKind.Discharged, Source = "HIS", OccurredAtUtc = DateTime.UtcNow, SourceEventKey = "a" };
        var open = new HospitalEvent { Id = Guid.NewGuid(), Kind = HospitalEventKind.Discharged, Source = "HIS", OccurredAtUtc = DateTime.UtcNow, SourceEventKey = "b" };
        await repo.RecordAsync(followed);
        await repo.RecordAsync(open);
        await repo.MarkFollowedUpAsync(followed.Id, DateTime.UtcNow);

        var worklist = await repo.ListNeedsFollowUpAsync(100);

        worklist.ShouldHaveSingleItem().Id.ShouldBe(open.Id);
    }

    private static ConsumeContext<T> Ctx<T>(T message) where T : class =>
        new(message, CancellationToken.None, new NoopBus());

    private sealed class InMemoryHospitalEvents : IHospitalEventRepository
    {
        public List<HospitalEvent> Rows { get; } = [];
        public Task RecordAsync(HospitalEvent e, CancellationToken cancellationToken = default)
        {
            if (!Rows.Any(r => r.Kind == e.Kind && r.SourceEventKey == e.SourceEventKey))
                Rows.Add(e);
            return Task.CompletedTask;
        }
        public Task<bool> MarkFollowedUpAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var row = Rows.FirstOrDefault(r => r.Id == id);
            if (row is null) return Task.FromResult(false);
            row.FollowedUp = true;
            row.FollowedUpAtUtc = nowUtc;
            return Task.FromResult(true);
        }
        public Task<IReadOnlyList<HospitalEvent>> ListNeedsFollowUpAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HospitalEvent>>([.. Rows.Where(r => !r.FollowedUp).OrderByDescending(r => r.OccurredAtUtc).Take(take)]);
        public Task<IReadOnlyList<HospitalEvent>> ListForPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HospitalEvent>>([.. Rows.Where(r => r.PatientId == patientId).Take(take)]);
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }
}
