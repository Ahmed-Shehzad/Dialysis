using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.Services;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Application.Features.IngestOruBatch;
using Dialysis.Treatment.Application.Features.IngestOruMessage;
using Dialysis.Treatment.Application.Features.RecordObservation;

using Dialysis.Treatment.Infrastructure;
using Dialysis.Treatment.Infrastructure.Hl7;
using Dialysis.Treatment.Infrastructure.Persistence;
using Dialysis.Treatment.Tests.TestDoubles;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using BuildingBlocks.TimeSync;

using Shouldly;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// End-to-end: FHS/BHS/ORU.../ORU.../BTS/FTS → IngestOruBatch → multiple sessions.
/// Uses Testcontainers PostgreSQL for real database behavior.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class OruBatchToSessionsIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public OruBatchToSessionsIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task OruBatch_TwoOruMessages_ProducesTwoSessionsAsync()
    {
        await using TreatmentDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new TreatmentSessionRepository(db, tenant);
        ITreatmentReadStore readStore = CreateReadStore();
        var sender = new BatchTestSender(repository);
        var batchHandler = new IngestOruBatchCommandHandler(new Hl7BatchParser(), sender);
        var getHandler = new GetTreatmentSessionQueryHandler(readStore, tenant);

        const string oru1 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01^ORU_R01|M001|P|2.5\r"
                            + "PID|||MRN1^^^^MR\r"
                            + "OBR|1||SESS001^DEV^EUI64|||20230215120000||||||start\r"
                            + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM\r";
        const string oru2 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120100||ORU^R01^ORU_R01|M002|P|2.5\r"
                            + "PID|||MRN2^^^^MR\r"
                            + "OBR|1||SESS002^DEV^EUI64|||20230215120100||||||start\r"
                            + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|350|ml/min^ml/min^UCUM\r";

        const string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru1}\r{oru2}\rBTS|2\rFTS|1\r";

        IngestOruBatchResponse response = await batchHandler.HandleAsync(new IngestOruBatchCommand(batch));

#pragma warning disable IDE0058
        response.ProcessedCount.ShouldBe(2);
        response.SessionIds.ShouldContain("SESS001");
        response.SessionIds.ShouldContain("SESS002");
#pragma warning restore IDE0058

        GetTreatmentSessionResponse? s1 = await getHandler.HandleAsync(new GetTreatmentSessionQuery(new BuildingBlocks.ValueObjects.SessionId("SESS001")));
        GetTreatmentSessionResponse? s2 = await getHandler.HandleAsync(new GetTreatmentSessionQuery(new BuildingBlocks.ValueObjects.SessionId("SESS002")));

#pragma warning disable IDE0058
        _ = s1.ShouldNotBeNull();
        _ = s2.ShouldNotBeNull();
        s1.PatientMrn.ShouldBe("MRN1");
        s2.PatientMrn.ShouldBe("MRN2");
        s1.Observations.Count.ShouldBe(1);
        s2.Observations.Count.ShouldBe(1);
#pragma warning restore IDE0058
    }

    private TreatmentDbContext CreateDbContext()
    {
        DbContextOptions<TreatmentDbContext> options = new DbContextOptionsBuilder<TreatmentDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new TreatmentDbContext(options);
    }

    private ITreatmentReadStore CreateReadStore()
    {
        DbContextOptions<TreatmentReadDbContext> options = new DbContextOptionsBuilder<TreatmentReadDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new TreatmentReadStore(new TreatmentReadDbContext(options));
    }

    private sealed class BatchTestSender : ISender
    {
        private readonly ITreatmentSessionRepository _repository;

        public BatchTestSender(ITreatmentSessionRepository repository) => _repository = repository;

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is IngestOruMessageCommand ingestCmd)
            {
                var ingestHandler = new IngestOruMessageCommandHandler(
                    this,
                    new OruR01Parser(),
                    new NoOpDeviceRegistrationClient(),
                    Options.Create(new TimeSyncOptions { MaxAllowedDriftSeconds = 0 }),
                    NullLogger<IngestOruMessageCommandHandler>.Instance);
                IngestOruMessageResponse result = await ingestHandler.HandleAsync(ingestCmd, cancellationToken);
                return (TResponse)(object)result;
            }

            if (request is RecordObservationCommand recordCmd)
            {
                var vitalSigns = new VitalSignsMonitoringService(NullLogger<VitalSignsMonitoringService>.Instance);
                var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
                var recordHandler = new RecordObservationCommandHandler(_repository, vitalSigns, new BuildingBlocks.Caching.NullCacheInvalidator(), tenant);
                RecordObservationResponse result = await recordHandler.HandleAsync(recordCmd, cancellationToken);
                return (TResponse)(object)result;
            }

            throw new NotSupportedException($"Request type {request?.GetType().Name} not supported");
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Non-generic SendAsync not supported");
    }
}
