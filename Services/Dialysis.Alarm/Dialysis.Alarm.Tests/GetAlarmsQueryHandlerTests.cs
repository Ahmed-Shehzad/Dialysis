using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.ValueObjects;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;
using Dialysis.Alarm.Application.Features.GetAlarms;
using Dialysis.Alarm.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Alarm.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class GetAlarmsQueryHandlerTests
{
    private readonly PostgreSqlFixture _fixture;

    public GetAlarmsQueryHandlerTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task HandleAsync_NoFilters_ReturnsAllAlarmsAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        AlarmDomain alarm = CreateAlarm("Venous Pressure High", "DEV1", "sess-001", DateTimeOffset.UtcNow.AddMinutes(-10));
        _ = db.Alarms.Add(alarm);
        _ = await db.SaveChangesAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new GetAlarmsQueryHandler(repository);

        var query = new GetAlarmsQuery();
        GetAlarmsResponse response = await handler.HandleAsync(query);

        response.Alarms.Count.ShouldBe(1);
        response.Alarms[0].AlarmType.ShouldBe("Venous Pressure High");
        response.Alarms[0].SessionId.ShouldBe("sess-001");
        response.Alarms[0].DeviceId.ShouldBe("DEV1");
    }

    [Fact]
    public async Task HandleAsync_FilterBySessionId_ReturnsMatchingAlarmsAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        _ = db.Alarms.Add(CreateAlarm("Alarm A", "DEV1", "sess-001", DateTimeOffset.UtcNow));
        _ = db.Alarms.Add(CreateAlarm("Alarm B", "DEV1", "sess-002", DateTimeOffset.UtcNow));
        _ = await db.SaveChangesAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new GetAlarmsQueryHandler(repository);

        var query = new GetAlarmsQuery(SessionId: "sess-001");
        GetAlarmsResponse response = await handler.HandleAsync(query);

        response.Alarms.Count.ShouldBe(1);
        response.Alarms[0].SessionId.ShouldBe("sess-001");
    }

    [Fact]
    public async Task HandleAsync_FilterByTimeRange_ReturnsAlarmsInRangeAsync()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        _ = db.Alarms.Add(CreateAlarm("Early", "DEV1", "sess-001", baseTime.AddHours(-3)));
        _ = db.Alarms.Add(CreateAlarm("InRange", "DEV1", "sess-001", baseTime.AddMinutes(-30)));
        _ = db.Alarms.Add(CreateAlarm("Late", "DEV1", "sess-001", baseTime.AddHours(1)));
        _ = await db.SaveChangesAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new GetAlarmsQueryHandler(repository);

        var query = new GetAlarmsQuery(SessionId: "sess-001", FromUtc: baseTime.AddHours(-1), ToUtc: baseTime);
        GetAlarmsResponse response = await handler.HandleAsync(query);

        response.Alarms.Count.ShouldBe(1);
        response.Alarms[0].AlarmType.ShouldBe("InRange");
    }

    [Fact]
    public async Task HandleAsync_NoAlarms_ReturnsEmptyAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new GetAlarmsQueryHandler(repository);

        var query = new GetAlarmsQuery();
        GetAlarmsResponse response = await handler.HandleAsync(query);

        response.Alarms.ShouldBeEmpty();
    }

    private AlarmDbContext CreateDbContext()
    {
        DbContextOptions<AlarmDbContext> options = new DbContextOptionsBuilder<AlarmDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new AlarmDbContext(options);
    }

    private static AlarmDomain CreateAlarm(string alarmType, string deviceId, string sessionId, DateTimeOffset occurredAt)
    {
        var state = new AlarmStateDescriptor(EventPhase.Start, AlarmState.Active, ActivityState.Enabled);
        var info = AlarmInfo.Create(new AlarmCreateParams(
            alarmType, "MDC_XYZ", "180-400",
            state, AlarmPriority.High, "ST", "H", alarmType,
            new DeviceId(deviceId), sessionId, occurredAt));
        return AlarmDomain.Raise(info, TenantContext.DefaultTenantId);
    }
}
