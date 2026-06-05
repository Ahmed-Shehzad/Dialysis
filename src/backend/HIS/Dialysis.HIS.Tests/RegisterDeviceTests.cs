using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.Integration.Features.DeviceRegistry;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>Handler + validator coverage for registering a device in the RPM registry.</summary>
public sealed class RegisterDeviceTests
{
    private readonly RegisterDeviceCommandValidator _validator = new();

    private static RegisterDeviceCommandHandler Handler(FakeDeviceRepository repo) =>
        new(repo, new DeviceTypeCatalog(DeviceTypeCatalog.Default), new NoopUnitOfWork());

    [Fact]
    public async Task Registers_A_Known_Type_With_Unique_Id_Async()
    {
        var repo = new FakeDeviceRepository();

        var id = await Handler(repo).HandleAsync(
            new RegisterDeviceCommand("OX-001", "pulse-oximeter", "Masimo", "Rad-97", "SN-1", null),
            CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        repo.Added.ShouldHaveSingleItem();
        repo.Added[0].DeviceId.ShouldBe("OX-001");
    }

    [Fact]
    public async Task Rejects_Unknown_Device_Type_Async()
    {
        var repo = new FakeDeviceRepository();

        await Should.ThrowAsync<DomainException>(() => Handler(repo).HandleAsync(
            new RegisterDeviceCommand("X-1", "teleporter", null, null, null, null),
            CancellationToken.None));
        repo.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_Duplicate_Device_Id_Async()
    {
        var repo = new FakeDeviceRepository();
        repo.Seed(Device.Register("OX-001", "pulse-oximeter", null, null, null, null, DateTime.UtcNow));

        await Should.ThrowAsync<DomainException>(() => Handler(repo).HandleAsync(
            new RegisterDeviceCommand("OX-001", "pulse-oximeter", null, null, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Validator_Rejects_Empty_Device_Id_Async()
    {
        var result = await _validator.ValidateAsync(
            new RegisterDeviceCommand(" ", "pulse-oximeter", null, null, null, null), CancellationToken.None);
        result.IsFailure.ShouldBeTrue();
    }

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        private readonly List<Device> _store = [];
        public List<Device> Added { get; } = [];

        public void Seed(Device device) => _store.Add(device);
        public void Add(Device device)
        {
            Added.Add(device);
            _store.Add(device);
        }

        public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(d => d.Id == id));

        public Task<Device?> FindByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.FirstOrDefault(d => d.DeviceId == deviceId));

        public Task<IReadOnlyList<Device>> ListAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Device>>([.. _store.Take(take)]);
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
