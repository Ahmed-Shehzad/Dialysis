using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Integration.Consumers;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

/// <summary>
/// Regression coverage for the HIS→EHR patient-mirroring consumers. The idempotency guard must key on
/// MRN as well as the HIS PatientId: the same patient can already exist in EHR under a *different* id
/// (direct registration, the sibling consumer, or a replayed/re-simulated event), and a second insert
/// would violate the unique <c>IX_Patients_MedicalRecordNumber</c> — the duplicate-key error this fixes.
/// </summary>
public sealed class EhrPatientFromHisMirrorConsumerTests
{
    [Fact]
    public async Task CheckIn_Skips_When_Mrn_Already_Present_Under_A_Different_Id_Async()
    {
        var repo = new FakeRepo(SeedPatient(Guid.CreateVersion7(), "MRN-DUP-1"));
        var uow = new CountingUow();
        var consumer = new EhrPatientFromHisCheckInConsumer(repo, uow, NullLogger<EhrPatientFromHisCheckInConsumer>.Instance);

        // Same MRN, different (HIS-supplied) id than the row already in EHR.
        var ev = new PatientCheckedInIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, 1, Guid.NewGuid(),
            PatientId: Guid.CreateVersion7(), PatientName: "Anna Müller", Mrn: "MRN-DUP-1", CheckedInAtUtc: DateTime.UtcNow);

        await consumer.HandleAsync(new ConsumeContext<PatientCheckedInIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

        repo.Count.ShouldBe(1);     // no duplicate row inserted
        uow.SaveCount.ShouldBe(0);  // and no write attempted
    }

    [Fact]
    public async Task WalkIn_Skips_When_Mrn_Already_Present_Under_A_Different_Id_Async()
    {
        var repo = new FakeRepo(SeedPatient(Guid.CreateVersion7(), "MRN-DUP-2"));
        var uow = new CountingUow();
        var consumer = new EhrPatientFromHisWalkInConsumer(repo, uow, NullLogger<EhrPatientFromHisWalkInConsumer>.Instance);

        var ev = new WalkInRegisteredIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, 1, Guid.NewGuid(),
            PatientId: Guid.CreateVersion7(), PatientName: "Bob Jones", Mrn: "MRN-DUP-2", EligibilityVerified: true, RegisteredAtUtc: DateTime.UtcNow);

        await consumer.HandleAsync(new ConsumeContext<WalkInRegisteredIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

        repo.Count.ShouldBe(1);
        uow.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task CheckIn_Creates_The_Patient_When_Both_Id_And_Mrn_Are_New_Async()
    {
        var repo = new FakeRepo();
        var uow = new CountingUow();
        var consumer = new EhrPatientFromHisCheckInConsumer(repo, uow, NullLogger<EhrPatientFromHisCheckInConsumer>.Instance);
        var newId = Guid.CreateVersion7();

        var ev = new PatientCheckedInIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, 1, Guid.NewGuid(),
            PatientId: newId, PatientName: "Carla Rossi", Mrn: "MRN-NEW-9", CheckedInAtUtc: DateTime.UtcNow);

        await consumer.HandleAsync(new ConsumeContext<PatientCheckedInIntegrationEvent>(ev, CancellationToken.None, new NoopBus()));

        repo.Count.ShouldBe(1);
        uow.SaveCount.ShouldBe(1);
        (await repo.FindByMedicalRecordNumberAsync("MRN-NEW-9")).ShouldNotBeNull();
    }

    private static Patient SeedPatient(Guid id, string mrn) =>
        Patient.Register(id, mrn, new HumanName("Existing", "Patient"), new DateOnly(1990, 1, 1), null, null);

    private sealed class FakeRepo : IPatientRepository
    {
        private readonly List<Patient> _store;
        public FakeRepo(params Patient[] seed) => _store = [.. seed];
        public int Count => _store.Count;
        public void Add(Patient patient) => _store.Add(patient);
        public Task<Patient?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(p => p.Id == id));
        public Task<Patient?> FindByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(p => p.MedicalRecordNumber == medicalRecordNumber));
        public Task<IReadOnlyList<Patient>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Patient>> SearchAsync(string? nameFragment, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<PatientSearchPage> SearchAsync(PatientSearchCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public IAsyncEnumerable<Patient> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CountingUow : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    }
}
