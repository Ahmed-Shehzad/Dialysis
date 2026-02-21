using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Dialysis.Alarm.Infrastructure;
using Dialysis.Alarm.Infrastructure.Hl7;
using Dialysis.Alarm.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

#pragma warning disable IDE0058

namespace Dialysis.Alarm.Tests;

/// <summary>End-to-end integration tests: parse ORU^R40 → RecordAlarm → persist → verify via GetAlarms. Uses Testcontainers PostgreSQL.</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class IngestOruR40IntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public IngestOruR40IntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task IngestOruR40_ParsesAndStoresAlarm_RetrievableViaReadStoreAsync()
    {
        await using AlarmDbContext db = CreateWriteDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, new BuildingBlocks.Caching.NullCacheInvalidator(), tenant);
        var parser = new OruR40Parser();

        const string oruR40 = """
            MSH|^~\&|MACH_EUI64|FAC|EMR|FAC|20230215120000||ORU^R40^ORU_R40|MSG001|P|2.6
            PID|||MRN123^^^^MR
            OBR|1||THERAPY001^MACH^EUI64
            OBX|1|ST|MDC_EVT_HI_VAL_ALARM^12345^MDC|1.1.3.1.1|MDC_PRESS_BLD_ART^150020^MDC|mmHg
            OBX|2|NM|MDC_PRESS_BLD_ART^12345^MDC|1.1.3.1.2|180|mmHg|||H|||20230215120000
            OBX|3|ST|MDC_ATTR_EVT_PHASE^68481^MDC|1.1.3.1.3|start
            OBX|4|ST|MDC_ATTR_ALARM_STATE^68482^MDC|1.1.3.1.4|active
            OBX|5|ST|MDC_ATTR_ALARM_INACTIVATION_STATE^68483^MDC|1.1.3.1.5|enabled
            """;

        OruR40ParseResult parseResult = parser.Parse(oruR40);
        parseResult.Alarms.Count.ShouldBe(1);
        parseResult.DeviceId.ShouldBe("MACH_EUI64");
        parseResult.SessionId.ShouldBe("THERAPY001");

        foreach (AlarmInfo alarm in parseResult.Alarms)
        {
            var command = new RecordAlarmCommand(alarm);
            RecordAlarmResponse response = await handler.HandleAsync(command);
            response.AlarmId.ShouldNotBeNullOrEmpty();
        }

        await using AlarmReadDbContext readDb = CreateReadDbContext();
        var readStore = new AlarmReadStore(readDb);
        IReadOnlyList<AlarmReadDto> alarms = await readStore.GetAlarmsAsync(tenant.TenantId, null, "THERAPY001", null, null);
        alarms.Count.ShouldBe(1);
        alarms[0].AlarmType.ShouldNotBeNullOrEmpty();
        alarms[0].SourceCode.ShouldBe("150020");
        alarms[0].EventPhase.ShouldBe("start");
    }

    [Fact]
    public async Task IngestOruR40_StartContinueEndLifecycle_UpdatesExistingAlarmAsync()
    {
        await using AlarmDbContext db = CreateWriteDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Alarms.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var handler = new RecordAlarmCommandHandler(repository, new BuildingBlocks.Caching.NullCacheInvalidator(), tenant);
        var parser = new OruR40Parser();

        const string start = """
            MSH|^~\&|DEV1|FAC|EMR|FAC|20230215120000||ORU^R40^ORU_R40|MSG1|P|2.6
            OBR|1||SESS-LIFE
            OBX|1|ST|196648^MDC_EVT_HI^MDC|1.0.0.0.1|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC
            OBX|2|NM|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.0.0.0.2|450|mm[Hg]|||20-400
            OBX|3|ST|68481^MDC_ATTR_EVT_PHASE^MDC|1.0.0.0.3|start
            OBX|4|ST|68482^MDC_ATTR_ALARM_STATE^MDC|1.0.0.0.4|active
            OBX|5|ST|68483^MDC_ATTR_ALARM_INACTIVATION_STATE^MDC|1.0.0.0.5|enabled
            """;

        const string end = """
            MSH|^~\&|DEV1|FAC|EMR|FAC|20230215120100||ORU^R40^ORU_R40|MSG2|P|2.6
            OBR|1||SESS-LIFE
            OBX|1|ST|196648^MDC_EVT_HI^MDC|1.0.0.0.1|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC
            OBX|2|NM|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.0.0.0.2|450|mm[Hg]|||20-400
            OBX|3|ST|68481^MDC_ATTR_EVT_PHASE^MDC|1.0.0.0.3|end
            OBX|4|ST|68482^MDC_ATTR_ALARM_STATE^MDC|1.0.0.0.4|inactive
            OBX|5|ST|68483^MDC_ATTR_ALARM_INACTIVATION_STATE^MDC|1.0.0.0.5|enabled
            """;

        foreach (AlarmInfo alarm in parser.Parse(start).Alarms)
            _ = await handler.HandleAsync(new RecordAlarmCommand(alarm));

        foreach (AlarmInfo alarm in parser.Parse(end).Alarms)
            _ = await handler.HandleAsync(new RecordAlarmCommand(alarm));

        await using AlarmReadDbContext readDb = CreateReadDbContext();
        var readStore = new AlarmReadStore(readDb);
        IReadOnlyList<AlarmReadDto> alarms = await readStore.GetAlarmsAsync(tenant.TenantId, null, "SESS-LIFE", null, null);
        alarms.Count.ShouldBe(1);
        alarms[0].EventPhase.ShouldBe("end");
        alarms[0].AlarmState.ShouldBe("inactive");
    }

    private AlarmDbContext CreateWriteDbContext()
    {
        DbContextOptions<AlarmDbContext> options = new DbContextOptionsBuilder<AlarmDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new AlarmDbContext(options);
    }

    private AlarmReadDbContext CreateReadDbContext()
    {
        DbContextOptions<AlarmReadDbContext> options = new DbContextOptionsBuilder<AlarmReadDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new AlarmReadDbContext(options);
    }
}
