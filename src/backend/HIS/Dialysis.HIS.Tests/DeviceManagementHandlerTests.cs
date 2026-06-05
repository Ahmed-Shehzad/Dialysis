using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.Integration.Features.DeviceRegistry;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>Wiring coverage for the device management handlers (bind, status transitions).</summary>
public sealed class DeviceManagementHandlerTests
{
    [Fact]
    public async Task Bind_Assigns_Patient_To_Registered_Device_Async()
    {
        var device = Device.Register("OX-1", "pulse-oximeter", null, null, null, null, DateTime.UtcNow);
        var repo = new FakeRepo(device);
        var patientId = Guid.NewGuid();

        await new BindDeviceToPatientCommandHandler(repo, new NoopUow())
            .HandleAsync(new BindDeviceToPatientCommand(device.Id, patientId, null), CancellationToken.None);

        device.PatientId.ShouldBe(patientId);
    }

    [Fact]
    public async Task Bind_Throws_When_Device_Not_Found_Async()
    {
        await Should.ThrowAsync<DomainException>(() =>
            new BindDeviceToPatientCommandHandler(new FakeRepo(), new NoopUow())
                .HandleAsync(new BindDeviceToPatientCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task Change_Status_Retire_Decommissions_Device_Async()
    {
        var device = Device.Register("OX-2", "pulse-oximeter", null, null, null, null, DateTime.UtcNow);
        var repo = new FakeRepo(device);

        await new ChangeDeviceStatusCommandHandler(repo, new NoopUow())
            .HandleAsync(new ChangeDeviceStatusCommand(device.Id, DeviceStatusAction.Retire), CancellationToken.None);

        device.Status.ShouldBe(DeviceStatus.Retired);
    }

    private sealed class FakeRepo : IDeviceRepository
    {
        private readonly List<Device> _store;
        public FakeRepo(params Device[] seed) => _store = [.. seed];
        public void Add(Device device) => _store.Add(device);
        public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(d => d.Id == id));
        public Task<Device?> FindByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(d => d.DeviceId == deviceId));
        public Task<IReadOnlyList<Device>> ListAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Device>>([.. _store.Take(take)]);
    }

    private sealed class NoopUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
