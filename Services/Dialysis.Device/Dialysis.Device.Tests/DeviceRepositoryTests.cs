using BuildingBlocks.Tenancy;
using Xunit;

using Dialysis.Device.Application.Features.GetDevice;
using Dialysis.Device.Application.Features.RegisterDevice;
using Dialysis.Device.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Device.Tests;

#pragma warning disable IDE0058 // Expression value is never used (Shouldly assertions)
public sealed class DeviceRepositoryTests
{
    [Fact]
    public async Task RegisterAndGet_Device_ReturnsDeviceAsync()
    {
        await using DeviceDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new DeviceRepository(db, tenant);
        var registerHandler = new RegisterDeviceCommandHandler(repository, tenant);
        var getHandler = new GetDeviceQueryHandler(repository);

        RegisterDeviceResponse registerResponse = await registerHandler.HandleAsync(new RegisterDeviceCommand(
            "MACH^EUI64^EUI-64",
            "Acme Corp",
            "HD-5000",
            "SN12345",
            null));

        Assert.True(registerResponse.Created);
        registerResponse.DeviceId.ShouldNotBeNullOrEmpty();

        GetDeviceResponse? device = await getHandler.HandleAsync(new GetDeviceQuery(registerResponse.DeviceId));
        device.ShouldNotBeNull();
        device.DeviceEui64.ShouldBe("MACH^EUI64^EUI-64");
        device.Manufacturer.ShouldBe("Acme Corp");
        device.Model.ShouldBe("HD-5000");
        device.Serial.ShouldBe("SN12345");
    }

    [Fact]
    public async Task RegisterTwice_SameEui64_UpdatesExistingAsync()
    {
        await using DeviceDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new DeviceRepository(db, tenant);
        var handler = new RegisterDeviceCommandHandler(repository, tenant);

        RegisterDeviceResponse first = await handler.HandleAsync(new RegisterDeviceCommand("DEV001", "M1", "MOD1"));
        Assert.True(first.Created);

        RegisterDeviceResponse second = await handler.HandleAsync(new RegisterDeviceCommand("DEV001", "M2", "MOD2"));
        second.Created.ShouldBeFalse();
        second.DeviceId.ShouldBe(first.DeviceId);

        GetDeviceResponse? device = await new GetDeviceQueryHandler(repository).HandleAsync(new GetDeviceQuery("DEV001"));
        device.ShouldNotBeNull();
        device.Manufacturer.ShouldBe("M2");
        device.Model.ShouldBe("MOD2");
    }

    private static DeviceDbContext CreateDbContext()
    {
        DbContextOptions<DeviceDbContext> options = new DbContextOptionsBuilder<DeviceDbContext>()
            .UseInMemoryDatabase("DeviceTests_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new DeviceDbContext(options);
    }
}
