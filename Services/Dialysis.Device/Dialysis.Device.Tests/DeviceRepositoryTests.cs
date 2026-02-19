using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;

using Dialysis.Device.Application.Abstractions;
using Dialysis.Device.Application.Features.GetDevice;
using Dialysis.Device.Application.Features.RegisterDevice;
using Dialysis.Device.Infrastructure;
using Dialysis.Device.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Xunit;

namespace Dialysis.Device.Tests;

#pragma warning disable IDE0058 // Expression value is never used (Shouldly assertions)
[Collection(PostgreSqlCollection.Name)]
public sealed class DeviceRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    public DeviceRepositoryTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RegisterAndGet_Device_ReturnsDeviceAsync()
    {
        await using DeviceDbContext db = CreateWriteDbContext();
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new DeviceRepository(db, tenant);
        var readStore = CreateReadStore();
        var registerHandler = new RegisterDeviceCommandHandler(repository, tenant);
        var getHandler = new GetDeviceQueryHandler(readStore, tenant);

        string deviceEui64 = DeviceTestData.DeviceEui64();
        string manufacturer = DeviceTestData.Manufacturer();
        string model = DeviceTestData.Model();
        string serial = DeviceTestData.SerialNumber();

        RegisterDeviceResponse registerResponse = await registerHandler.HandleAsync(new RegisterDeviceCommand(
            deviceEui64,
            manufacturer,
            model,
            serial,
            null));

        Assert.True(registerResponse.Created);
        registerResponse.DeviceId.ShouldNotBeNullOrEmpty();

        GetDeviceResponse? device = await getHandler.HandleAsync(new GetDeviceQuery(registerResponse.DeviceId));
        _ = device.ShouldNotBeNull();
        device.DeviceEui64.ShouldBe(deviceEui64);
        device.Manufacturer.ShouldBe(manufacturer);
        device.Model.ShouldBe(model);
        device.Serial.ShouldBe(serial);
    }

    [Fact]
    public async Task RegisterTwice_SameEui64_UpdatesExistingAsync()
    {
        string deviceEui64 = DeviceTestData.DeviceId();
        string manufacturer1 = DeviceTestData.Manufacturer();
        string model1 = DeviceTestData.Model();
        string manufacturer2 = DeviceTestData.Manufacturer();
        string model2 = DeviceTestData.Model();

        await using DeviceDbContext db = CreateWriteDbContext();
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new DeviceRepository(db, tenant);
        var readStore = CreateReadStore();
        var handler = new RegisterDeviceCommandHandler(repository, tenant);

        RegisterDeviceResponse first = await handler.HandleAsync(new RegisterDeviceCommand(deviceEui64, manufacturer1, model1));
        Assert.True(first.Created);

        RegisterDeviceResponse second = await handler.HandleAsync(new RegisterDeviceCommand(deviceEui64, manufacturer2, model2));
        second.Created.ShouldBeFalse();
        second.DeviceId.ShouldBe(first.DeviceId);

        GetDeviceResponse? device = await new GetDeviceQueryHandler(readStore, tenant).HandleAsync(new GetDeviceQuery(deviceEui64));
        _ = device.ShouldNotBeNull();
        device.Manufacturer.ShouldBe(manufacturer2);
        device.Model.ShouldBe(model2);
    }

    private DeviceDbContext CreateWriteDbContext()
    {
        DbContextOptions<DeviceDbContext> options = new DbContextOptionsBuilder<DeviceDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new DeviceDbContext(options);
    }

    private IDeviceReadStore CreateReadStore()
    {
        DbContextOptions<DeviceReadDbContext> options = new DbContextOptionsBuilder<DeviceReadDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new DeviceReadStore(new DeviceReadDbContext(options));
    }
}
