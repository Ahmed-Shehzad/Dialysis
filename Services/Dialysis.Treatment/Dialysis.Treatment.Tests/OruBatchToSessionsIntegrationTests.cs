using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Application.Features.IngestOruBatch;
using Dialysis.Treatment.Application.Features.IngestOruMessage;
using Dialysis.Treatment.Application.Features.RecordObservation;

using Dialysis.Treatment.Infrastructure.Hl7;
using Dialysis.Treatment.Infrastructure.Persistence;
using Dialysis.Treatment.Tests.TestDoubles;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Task = System.Threading.Tasks.Task;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// End-to-end: FHS/BHS/ORU.../ORU.../BTS/FTS → IngestOruBatch → multiple sessions.
/// </summary>
public sealed class OruBatchToSessionsIntegrationTests
{
    [Fact]
    public async Task OruBatch_TwoOruMessages_ProducesTwoSessionsAsync()
    {
        await using TreatmentDbContext db = CreateDbContext();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new TreatmentSessionRepository(db, tenant);
        var sender = new BatchTestSender(repository);
        var batchHandler = new IngestOruBatchCommandHandler(new Hl7BatchParser(), sender);
        var getHandler = new GetTreatmentSessionQueryHandler(repository);

        string oru1 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01^ORU_R01|M001|P|2.5\r"
                      + "PID|||MRN1^^^^MR\r"
                      + "OBR|1||SESS001^DEV^EUI64|||20230215120000||||||start\r"
                      + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM\r";
        string oru2 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120100||ORU^R01^ORU_R01|M002|P|2.5\r"
                      + "PID|||MRN2^^^^MR\r"
                      + "OBR|1||SESS002^DEV^EUI64|||20230215120100||||||start\r"
                      + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|350|ml/min^ml/min^UCUM\r";

        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru1}\r{oru2}\rBTS|2\rFTS|1\r";

        IngestOruBatchResponse response = await batchHandler.HandleAsync(new IngestOruBatchCommand(batch));

#pragma warning disable IDE0058
        response.ProcessedCount.ShouldBe(2);
        response.SessionIds.ShouldContain("SESS001");
        response.SessionIds.ShouldContain("SESS002");
#pragma warning restore IDE0058

        GetTreatmentSessionResponse? s1 = await getHandler.HandleAsync(new GetTreatmentSessionQuery(new SessionId("SESS001")));
        GetTreatmentSessionResponse? s2 = await getHandler.HandleAsync(new GetTreatmentSessionQuery(new SessionId("SESS002")));

#pragma warning disable IDE0058
        s1.ShouldNotBeNull();
        s2.ShouldNotBeNull();
        s1.PatientMrn.ShouldBe("MRN1");
        s2.PatientMrn.ShouldBe("MRN2");
        s1.Observations.Count.ShouldBe(1);
        s2.Observations.Count.ShouldBe(1);
#pragma warning restore IDE0058
    }

    private static TreatmentDbContext CreateDbContext()
    {
        DbContextOptions<TreatmentDbContext> options = new DbContextOptionsBuilder<TreatmentDbContext>()
            .UseInMemoryDatabase("OruBatch_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new TreatmentDbContext(options);
    }

    private sealed class BatchTestSender : ISender
    {
        private readonly ITreatmentSessionRepository _repository;

        public BatchTestSender(ITreatmentSessionRepository repository) => _repository = repository;

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is IngestOruMessageCommand ingestCmd)
            {
                var ingestHandler = new IngestOruMessageCommandHandler(this, new OruR01Parser(), new NoOpDeviceRegistrationClient());
                IngestOruMessageResponse result = await ingestHandler.HandleAsync(ingestCmd, cancellationToken);
                return (TResponse)(object)result;
            }

            if (request is RecordObservationCommand recordCmd)
            {
                var recordHandler = new RecordObservationCommandHandler(_repository);
                RecordObservationResponse result = await recordHandler.HandleAsync(recordCmd, cancellationToken);
                return (TResponse)(object)result;
            }

            throw new NotSupportedException($"Request type {request?.GetType().Name} not supported");
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Non-generic SendAsync not supported");
    }
}
