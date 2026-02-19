using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Application.Domain.ValueObjects;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Dialysis.Alarm.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Alarm.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class RecordAlarmCommandHandlerTests
{
    private readonly PostgreSqlFixture _fixture;

    public RecordAlarmCommandHandlerTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task HandleAsync_Start_AlwaysCreatesNewAlarmAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, tenant);

        AlarmInfo info = CreateAlarmInfo(EventPhase.Start, "active", "MDC_PUMP_VEN");
        var command = new RecordAlarmCommand(info);
        RecordAlarmResponse response = await handler.HandleAsync(command);

        response.AlarmId.ShouldNotBeNullOrEmpty();
        Application.Domain.Alarm saved = (await db.Alarms.FirstOrDefaultAsync(a => a.Id == Ulid.Parse(response.AlarmId))).ShouldNotBeNull();
        saved.SourceCode.ShouldBe("MDC_PUMP_VEN");
        saved.EventPhase.Value.ShouldBe("start");
        saved.AlarmState.Value.ShouldBe("active");
    }

    [Fact]
    public async Task HandleAsync_Continue_UpdatesExistingActiveAlarmAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, tenant);

        var deviceId = new DeviceId("DEV1");
        string sessionId = "sess-001";
        string sourceCode = "MDC_PUMP_VEN";
        DateTimeOffset originalTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        AlarmInfo startInfo = CreateAlarmInfo(EventPhase.Start, "active", sourceCode, deviceId, sessionId, originalTime);
#pragma warning disable IDE0058
        _ = await handler.HandleAsync(new RecordAlarmCommand(startInfo));
#pragma warning restore IDE0058

        DateTimeOffset continueTime = DateTimeOffset.UtcNow.AddMinutes(-2);
        AlarmInfo continueInfo = CreateAlarmInfo(EventPhase.Continue, "active", sourceCode, deviceId, sessionId, continueTime);
        RecordAlarmResponse continueResponse = await handler.HandleAsync(new RecordAlarmCommand(continueInfo));

        continueResponse.AlarmId.ShouldNotBeNullOrEmpty();
        int count = await db.Alarms.CountAsync();
        count.ShouldBe(1);

        Application.Domain.Alarm alarm = await db.Alarms.FirstAsync();
        alarm.OccurredAt.ShouldBe(continueTime);
        alarm.EventPhase.Value.ShouldBe("continue");
    }

    [Fact]
    public async Task HandleAsync_End_UpdatesExistingAlarmToInactiveAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, tenant);

        var deviceId = new DeviceId("DEV1");
        string sessionId = "sess-001";
        string sourceCode = "MDC_PUMP_VEN";
        AlarmInfo startInfo = CreateAlarmInfo(EventPhase.Start, "active", sourceCode, deviceId, sessionId, DateTimeOffset.UtcNow.AddMinutes(-5));
#pragma warning disable IDE0058
        _ = await handler.HandleAsync(new RecordAlarmCommand(startInfo));
#pragma warning restore IDE0058

        AlarmInfo endInfo = CreateAlarmInfo(EventPhase.End, "inactive", sourceCode, deviceId, sessionId, DateTimeOffset.UtcNow);
        RecordAlarmResponse endResponse = await handler.HandleAsync(new RecordAlarmCommand(endInfo));

        endResponse.AlarmId.ShouldNotBeNullOrEmpty();
        Application.Domain.Alarm alarm = await db.Alarms.FirstAsync();
        alarm.AlarmState.Value.ShouldBe("inactive");
        alarm.EventPhase.Value.ShouldBe("end");
    }

    [Fact]
    public async Task HandleAsync_End_NoExistingAlarm_ReturnsEmptyIdAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, tenant);

        AlarmInfo endInfo = CreateAlarmInfo(EventPhase.End, "inactive", "MDC_PUMP_VEN", new DeviceId("DEV1"), "sess-001", DateTimeOffset.UtcNow);
        RecordAlarmResponse response = await handler.HandleAsync(new RecordAlarmCommand(endInfo));

        response.AlarmId.ShouldBe(string.Empty);
        int count = await db.Alarms.CountAsync();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_Continue_NoExistingAlarm_CreatesOrphanAlarmAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, tenant);

        AlarmInfo continueInfo = CreateAlarmInfo(EventPhase.Continue, "active", "MDC_PUMP_VEN", new DeviceId("DEV1"), "sess-001", DateTimeOffset.UtcNow);
        RecordAlarmResponse response = await handler.HandleAsync(new RecordAlarmCommand(continueInfo));

        response.AlarmId.ShouldNotBeNullOrEmpty();
        int count = await db.Alarms.CountAsync();
        count.ShouldBe(1);
    }

    private static AlarmInfo CreateAlarmInfo(EventPhase phase, string alarmState, string sourceCode,
        DeviceId? deviceId = null, string? sessionId = null, DateTimeOffset? occurredAt = null)
    {
        var state = new AlarmStateDescriptor(phase, new AlarmState(alarmState), ActivityState.Enabled);
        var @params = new AlarmCreateParams(
            "Test Alarm", sourceCode, "100-500", state, null, null, null, "Test Alarm",
            deviceId ?? new DeviceId("DEV1"), new BuildingBlocks.ValueObjects.SessionId(sessionId ?? "sess-001"), occurredAt ?? DateTimeOffset.UtcNow);
        return AlarmInfo.Create(@params);
    }

    private AlarmDbContext CreateDbContext()
    {
        DbContextOptions<AlarmDbContext> options = new DbContextOptionsBuilder<AlarmDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new AlarmDbContext(options);
    }
}
