using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.DomainDrivenDesign.Specifications;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Consumers;
using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.Medications.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Medications;

/// <summary>
/// Closes the MAR → inventory loop. The consumer must deduct one unit from the matching
/// inventory row, must not throw when no row matches, and must pick the lot with the
/// longest shelf life when multiple rows match.
/// </summary>
public sealed class OnMedicationAdministeredTests
{
    [Fact]
    public async Task Matching_Inventory_Row_Deducts_One_Unit_Async()
    {
        var item = new MedicationInventoryItem(
            id: Guid.NewGuid(),
            medication: MedicationCoding.RxNorm("1234", "Heparin 5000IU"),
            lotNumber: "LOT-A",
            expiryUtc: new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            initialOnHand: 10,
            threshold: 5);
        var inventory = new StubInventoryRepo([item]);
        var consumer = new OnMedicationAdministered(inventory, new StubUnitOfWork(),
            NullLogger<OnMedicationAdministered>.Instance);

        await consumer.HandleAsync(MakeContext(Event(rxnormCode: "1234")));

        item.OnHandUnits.ShouldBe(9);
    }

    [Fact]
    public async Task Multiple_Lots_Picks_The_Longest_Shelf_Life_Async()
    {
        var shortDated = new MedicationInventoryItem(
            Guid.NewGuid(), MedicationCoding.RxNorm("1234", "Heparin"),
            "LOT-SHORT", new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 5, 1);
        var longDated = new MedicationInventoryItem(
            Guid.NewGuid(), MedicationCoding.RxNorm("1234", "Heparin"),
            "LOT-LONG", new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc), 5, 1);
        var inventory = new StubInventoryRepo([shortDated, longDated]);
        var consumer = new OnMedicationAdministered(inventory, new StubUnitOfWork(),
            NullLogger<OnMedicationAdministered>.Instance);

        await consumer.HandleAsync(MakeContext(Event(rxnormCode: "1234")));

        longDated.OnHandUnits.ShouldBe(4);
        shortDated.OnHandUnits.ShouldBe(5);
    }

    [Fact]
    public async Task Missing_Inventory_Row_Does_Not_Throw_Async()
    {
        var inventory = new StubInventoryRepo([]);
        var consumer = new OnMedicationAdministered(inventory, new StubUnitOfWork(),
            NullLogger<OnMedicationAdministered>.Instance);

        await Should.NotThrowAsync(() => consumer.HandleAsync(MakeContext(Event(rxnormCode: "9999"))));
    }

    private static MedicationAdministeredIntegrationEvent Event(string rxnormCode) => new()
    {
        EntryId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        MedicationCodeSystem = "http://www.nlm.nih.gov/research/umls/rxnorm",
        MedicationCode = rxnormCode,
        MedicationDisplay = "Test medication",
        DoseQuantity = 1m,
        DoseUnit = "U",
        Route = "Intravenous",
        AdministeredAtUtc = DateTime.UtcNow,
        AdministeredBySub = "nurse-1",
    };

    private static ConsumeContext<MedicationAdministeredIntegrationEvent> MakeContext(
        MedicationAdministeredIntegrationEvent message)
        => new(message, CancellationToken.None, NullBus.Instance);

    private sealed class StubInventoryRepo(List<MedicationInventoryItem> seed)
        : IPdmsRepository<MedicationInventoryItem, Guid>
    {
        public Task<MedicationInventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(seed.FirstOrDefault(i => i.Id == id));
        public Task<IReadOnlyList<MedicationInventoryItem>> ListAsync(
            ISpecification<MedicationInventoryItem>? specification = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MedicationInventoryItem>>(seed);
        public Task AddAsync(MedicationInventoryItem aggregate, CancellationToken cancellationToken = default)
        {
            seed.Add(aggregate);
            return Task.CompletedTask;
        }
        public void Update(MedicationInventoryItem aggregate) { }
        public void Remove(MedicationInventoryItem aggregate) => seed.Remove(aggregate);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    private sealed class NullBus : ITransponderBus
    {
        public static NullBus Instance { get; } = new();
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishAsync<T>(T message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<T>(T message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }
}
