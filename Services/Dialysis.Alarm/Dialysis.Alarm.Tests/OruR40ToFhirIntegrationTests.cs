using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Features.GetAlarms;
using Dialysis.Alarm.Application.Features.IngestOruR40Message;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Dialysis.Alarm.Infrastructure.Hl7;
using Dialysis.Alarm.Infrastructure.Persistence;

using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Alarm.Tests;

/// <summary>
/// End-to-end: ORU^R40 → IngestOruR40 → RecordAlarm → GetAlarms → FHIR DetectedIssue Bundle.
/// </summary>
public sealed class OruR40ToFhirIntegrationTests
{
    [Fact]
    public async Task OruR40_IngestAndRecord_ProducesFhirBundleWithDetectedIssueAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var ingestHandler = new IngestOruR40MessageCommandHandler(new RecordAlarmSender(repository), new OruR40Parser());
        var getHandler = new GetAlarmsQueryHandler(repository);

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

        IngestOruR40MessageResponse ingestResponse = await ingestHandler.HandleAsync(new IngestOruR40MessageCommand(oruR40));

#pragma warning disable IDE0058
        ingestResponse.AlarmCount.ShouldBe(1);
        ingestResponse.AlarmIds.Count.ShouldBe(1);
#pragma warning restore IDE0058

        GetAlarmsResponse alarmsResponse = await getHandler.HandleAsync(new GetAlarmsQuery(DeviceId: null, SessionId: "THERAPY001", FromUtc: null, ToUtc: null));
        alarmsResponse.Alarms.Count.ShouldBe(1);

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = []
        };

        foreach (AlarmDto dto in alarmsResponse.Alarms)
        {
            var input = new AlarmMappingInput(
                dto.AlarmType,
                dto.SourceCode,
                dto.SourceLimits,
                dto.EventPhase,
                dto.AlarmState,
                dto.ActivityState,
                dto.Priority,
                dto.InterpretationType,
                null,
                dto.DeviceId,
                dto.SessionId,
                dto.OccurredAt);
            DetectedIssue issue = AlarmMapper.ToFhirDetectedIssue(input);
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:alarm-{dto.Id}",
                Resource = issue
            });
        }

#pragma warning disable IDE0058
        bundle.Entry.Count.ShouldBe(1);
        bundle.Entry[0].Resource.ShouldBeOfType<DetectedIssue>();
        var di = (DetectedIssue)bundle.Entry[0].Resource!;
        di.Code.ShouldNotBeNull();
        di.Code.Coding.ShouldContain(c => c.System == "urn:iso:std:iso:11073:10101");
        di.Severity.ShouldBeOneOf(DetectedIssue.DetectedIssueSeverity.High, DetectedIssue.DetectedIssueSeverity.Moderate, DetectedIssue.DetectedIssueSeverity.Low);
        (di.Detail ?? string.Empty).ShouldContain("start");
        (di.Detail ?? string.Empty).ShouldContain("active");
#pragma warning restore IDE0058
    }

    private static AlarmDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AlarmDbContext>()
            .UseInMemoryDatabase("OruR40ToFhir_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AlarmDbContext(options);
    }

    private sealed class RecordAlarmSender : ISender
    {
        private readonly IAlarmRepository _repository;

        public RecordAlarmSender(IAlarmRepository repository) => _repository = repository;

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is RecordAlarmCommand cmd)
            {
                var handler = new RecordAlarmCommandHandler(_repository, new TenantContext { TenantId = TenantContext.DefaultTenantId });
                var result = await handler.HandleAsync(cmd, cancellationToken);
                return (TResponse)(object)result;
            }
            throw new NotSupportedException($"Request type {request?.GetType().Name} not supported");
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Non-generic SendAsync not supported");
    }
}
