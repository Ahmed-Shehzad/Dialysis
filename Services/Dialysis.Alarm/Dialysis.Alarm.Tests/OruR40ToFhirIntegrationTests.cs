using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;

using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Features.GetAlarms;
using Dialysis.Alarm.Application.Features.IngestOruR40Message;
using Dialysis.Alarm.Application.Features.RecordAlarm;

using Dialysis.Alarm.Infrastructure.Hl7;
using Dialysis.Alarm.Infrastructure.Persistence;
using Dialysis.Alarm.Tests.TestDoubles;

using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Alarm.Tests;

/// <summary>
/// End-to-end: ORU^R40 → IngestOruR40 → RecordAlarm → GetAlarms → FHIR DetectedIssue Bundle. Uses Testcontainers PostgreSQL.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class OruR40ToFhirIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public OruR40ToFhirIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task OruR40_IngestAndRecord_ProducesFhirBundleWithDetectedIssueAsync()
    {
        await using AlarmDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new AlarmRepository(db, tenant);
        var ingestHandler = new IngestOruR40MessageCommandHandler(new RecordAlarmSender(repository), new OruR40Parser(), new NoOpDeviceRegistrationClient());
        var getHandler = new GetAlarmsQueryHandler(repository);

        string mrn = AlarmTestData.Mrn();
        string sessionId = AlarmTestData.SessionId();
        string oruR40 = AlarmTestData.OruR40(mrn, sessionId);

        IngestOruR40MessageResponse ingestResponse = await ingestHandler.HandleAsync(new IngestOruR40MessageCommand(oruR40));

#pragma warning disable IDE0058
        ingestResponse.AlarmCount.ShouldBe(1);
        ingestResponse.AlarmIds.Count.ShouldBe(1);
#pragma warning restore IDE0058

        GetAlarmsResponse alarmsResponse = await getHandler.HandleAsync(new GetAlarmsQuery(DeviceId: null, SessionId: sessionId, FromUtc: null, ToUtc: null));
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
        _ = bundle.Entry[0].Resource.ShouldBeOfType<DetectedIssue>();
        var di = (DetectedIssue)bundle.Entry[0].Resource!;
        _ = di.Code.ShouldNotBeNull();
        di.Code.Coding.ShouldContain(c => c.System == "urn:iso:std:iso:11073:10101");
        di.Severity.ShouldBeOneOf(DetectedIssue.DetectedIssueSeverity.High, DetectedIssue.DetectedIssueSeverity.Moderate, DetectedIssue.DetectedIssueSeverity.Low);
        (di.Detail ?? string.Empty).ShouldContain("start");
        (di.Detail ?? string.Empty).ShouldContain("active");
#pragma warning restore IDE0058
    }

    private AlarmDbContext CreateDbContext()
    {
        DbContextOptions<AlarmDbContext> options = new DbContextOptionsBuilder<AlarmDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
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
                RecordAlarmResponse result = await handler.HandleAsync(cmd, cancellationToken);
                return (TResponse)(object)result;
            }
            throw new NotSupportedException($"Request type {request?.GetType().Name} not supported");
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Non-generic SendAsync not supported");
    }
}
