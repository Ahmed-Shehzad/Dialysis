using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>
/// Coverage for ingestion governed by the device registry: registered devices are status- and
/// binding-checked and their last-seen is stamped; unknown devices are accepted (default) or
/// rejected (strict mode).
/// </summary>
public sealed class DeviceIngestionGovernanceTests
{
    private static IngestDeviceReadingCommandHandler Handler(
        FakeDeviceRegistry registry, bool requireRegistration) =>
        new(
            new SlidingWindowRateLimiter(maxEventsPerWindow: 1000, window: TimeSpan.FromMinutes(1)),
            new FakeReadingRepository(),
            registry,
            Options.Create(new DeviceIngestionOptions { RequireRegistration = requireRegistration }));

    private static IngestDeviceReadingCommand Reading(string deviceId, Guid patientId) =>
        new(deviceId, patientId, "{\"v\":1}", ExternalMessageId: null);

    [Fact]
    public async Task Unknown_Device_Is_Accepted_When_Registration_Not_Required_Async()
    {
        var id = await Handler(new FakeDeviceRegistry(), requireRegistration: false)
            .HandleAsync(Reading("ghost-1", Guid.NewGuid()), CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Unknown_Device_Is_Rejected_In_Strict_Mode_Async()
    {
        await Should.ThrowAsync<DomainException>(() =>
            Handler(new FakeDeviceRegistry(), requireRegistration: true)
                .HandleAsync(Reading("ghost-2", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Registered_Device_Is_Stamped_Seen_And_Activated_Async()
    {
        var registry = new FakeDeviceRegistry();
        var device = Device.Register("OX-1", "pulse-oximeter", null, null, null, null, DateTime.UtcNow);
        registry.Seed(device);

        await Handler(registry, requireRegistration: true)
            .HandleAsync(Reading("OX-1", Guid.NewGuid()), CancellationToken.None);

        device.Status.ShouldBe(DeviceStatus.Active);
        device.LastSeenAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Suspended_Device_Reading_Is_Rejected_Async()
    {
        var registry = new FakeDeviceRegistry();
        var device = Device.Register("OX-2", "pulse-oximeter", null, null, null, null, DateTime.UtcNow);
        device.Suspend();
        registry.Seed(device);

        await Should.ThrowAsync<DomainException>(() =>
            Handler(registry, requireRegistration: false)
                .HandleAsync(Reading("OX-2", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Bound_Device_Rejects_Reading_For_A_Different_Patient_Async()
    {
        var registry = new FakeDeviceRegistry();
        var boundPatient = Guid.NewGuid();
        var device = Device.Register("OX-3", "pulse-oximeter", null, null, null, null, DateTime.UtcNow);
        device.BindToPatient(boundPatient, null);
        registry.Seed(device);

        await Should.ThrowAsync<DomainException>(() =>
            Handler(registry, requireRegistration: false)
                .HandleAsync(Reading("OX-3", Guid.NewGuid()), CancellationToken.None));

        // Same patient is accepted.
        await Handler(registry, requireRegistration: false)
            .HandleAsync(Reading("OX-3", boundPatient), CancellationToken.None);
        device.LastSeenAtUtc.ShouldNotBeNull();
    }

    private sealed class FakeDeviceRegistry : IDeviceRepository
    {
        private readonly List<Device> _store = [];
        public void Seed(Device device) => _store.Add(device);
        public void Add(Device device) => _store.Add(device);
        public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(d => d.Id == id));
        public Task<Device?> FindByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(d => d.DeviceId == deviceId));
        public Task<IReadOnlyList<Device>> ListAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Device>>([.. _store.Take(take)]);
    }

    private sealed class FakeReadingRepository : IDeviceReadingRepository
    {
        public Task<Guid?> FindIdByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);
        public Task<Guid> PersistIdempotentAsync(DeviceReadingRecord record, CancellationToken cancellationToken = default) =>
            Task.FromResult(record.Id);
    }
}
