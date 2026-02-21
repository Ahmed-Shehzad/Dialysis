using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;

using Dialysis.Hl7ToFhir;
using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.Services;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Application.Features.IngestOruMessage;
using Dialysis.Treatment.Application.Features.RecordObservation;

using Dialysis.Treatment.Infrastructure;
using Dialysis.Treatment.Infrastructure.Hl7;
using Dialysis.Treatment.Infrastructure.Persistence;
using Dialysis.Treatment.Tests.TestDoubles;

using Hl7.Fhir.Model;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using BuildingBlocks.TimeSync;

using Task = System.Threading.Tasks.Task;

using Shouldly;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// End-to-end integration tests: parse ORU^R01 → IngestOruMessage → RecordObservation → GetTreatmentSession → FHIR Bundle.
/// Uses Testcontainers PostgreSQL for real database behavior.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class OruR01ToFhirIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public OruR01ToFhirIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task OruR01_IngestAndRecord_ProducesFhirBundleWithProcedureAndObservationsAsync()
    {
        await using TreatmentDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Observations.ExecuteDeleteAsync();
        _ = await db.TreatmentSessions.ExecuteDeleteAsync();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new TreatmentSessionRepository(db, tenant);
        ITreatmentReadStore readStore = CreateReadStore();
        var ingestHandler = new IngestOruMessageCommandHandler(
            new Sender(repository),
            new OruR01Parser(),
            new NoOpDeviceRegistrationClient(),
            Options.Create(new TimeSyncOptions { MaxAllowedDriftSeconds = 0 }),
            NullLogger<IngestOruMessageCommandHandler>.Instance);
        var getHandler = new GetTreatmentSessionQueryHandler(readStore, tenant);

        string mrn = TreatmentTestData.Mrn();
        string sessionId = TreatmentTestData.SessionId();
        string oruR01 = TreatmentTestData.OruR01(mrn, sessionId);

#pragma warning disable IDE0058
        IngestOruMessageResponse ingestResponse = await ingestHandler.HandleAsync(new IngestOruMessageCommand(oruR01));
        ingestResponse.ObservationCount.ShouldBe(2);
        ingestResponse.SessionId.ShouldBe(sessionId);

        GetTreatmentSessionResponse? sessionResponse = await getHandler.HandleAsync(new GetTreatmentSessionQuery(new BuildingBlocks.ValueObjects.SessionId(sessionId)));
        _ = sessionResponse.ShouldNotBeNull();
        sessionResponse.Observations.Count.ShouldBe(2);
#pragma warning restore IDE0058

        Procedure procedure = ProcedureMapper.ToFhirProcedure(
            sessionResponse.SessionId,
            sessionResponse.PatientMrn,
            sessionResponse.DeviceId,
            sessionResponse.Status,
            sessionResponse.StartedAt,
            sessionResponse.EndedAt);
        procedure.Id = $"proc-{sessionResponse.SessionId}";

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry =
            [
                new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:procedure-{sessionResponse.SessionId}",
                    Resource = procedure
                }
            ]
        };

        foreach (ObservationDto obs in sessionResponse.Observations)
        {
            string obsFullUrl = $"urn:uuid:obs-{obs.Code}-{obs.SubId ?? "0"}";
            var input = new ObservationMappingInput(
                obs.Code,
                obs.Value,
                obs.Unit,
                obs.SubId,
                obs.ReferenceRange,
                obs.Provenance,
                obs.EffectiveTime,
                sessionResponse.DeviceId,
                sessionResponse.PatientMrn,
                obs.ChannelName);
            Observation fhirObs = ObservationMapper.ToFhirObservation(input);
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = obsFullUrl,
                Resource = fhirObs
            });

            if (!string.IsNullOrEmpty(obs.Provenance))
            {
                DateTimeOffset occurredAt = obs.EffectiveTime ?? sessionResponse.StartedAt ?? DateTimeOffset.UtcNow;
                Provenance prov = ProvenanceMapper.ToFhirProvenance(obsFullUrl, obs.Provenance, occurredAt, sessionResponse.DeviceId);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:prov-{obs.Code}-{obs.SubId ?? "0"}",
                    Resource = prov
                });
            }
        }

#pragma warning disable IDE0058
        bundle.Entry.Count.ShouldBe(5); // 1 Procedure + 2 Observations + 2 Provenance

        Resource? res0 = bundle.Entry[0].Resource;
        _ = res0.ShouldNotBeNull();
        _ = res0.ShouldBeOfType<Procedure>();
        ((Procedure)res0!).Code!.Text.ShouldBe("Hemodialysis");

        Observation? obs152348 = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Observation>()
            .FirstOrDefault(o => o.Code != null && o.Code.Coding != null && o.Code.Coding.Any(c => c.Code == "152348"));
        _ = obs152348.ShouldNotBeNull();
        _ = obs152348!.Value.ShouldBeOfType<Quantity>();
        ((Quantity)(obs152348.Value ?? throw new InvalidOperationException("obs152348.Value is null"))).Value.ShouldBe(300);

        Observation? obs158776 = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Observation>()
            .FirstOrDefault(o => o.Code != null && o.Code.Coding != null && o.Code.Coding.Any(c => c.Code == "158776"));
        _ = obs158776.ShouldNotBeNull();
        _ = obs158776!.Value.ShouldBeOfType<Quantity>();
        ((Quantity)(obs158776.Value ?? throw new InvalidOperationException("obs158776.Value is null"))).Value.ShouldBe(120);
        obs158776.ReferenceRange.ShouldContain(r => r.Text == "80-200");

        var provenanceEntries = bundle.Entry.Select(e => e.Resource).OfType<Provenance>().ToList();
        provenanceEntries.Count.ShouldBe(2);
        provenanceEntries.ShouldContain(p => p.Target != null && p.Target.Any(t => t.Reference == "urn:uuid:obs-152348-1.1.3.1"));
        provenanceEntries.ShouldContain(p => p.Target != null && p.Target.Any(t => t.Reference == "urn:uuid:obs-158776-1.1.3.2"));
        provenanceEntries.First(p => p.Target != null && p.Target.Any(t => t.Reference == "urn:uuid:obs-152348-1.1.3.1"))
            .Activity!.Text.ShouldBe("Automatic measurement");
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

    private sealed class Sender : ISender
    {
        private readonly ITreatmentSessionRepository _repository;

        public Sender(ITreatmentSessionRepository repository) => _repository = repository;

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is RecordObservationCommand cmd)
            {
                var vitalSigns = new VitalSignsMonitoringService(NullLogger<VitalSignsMonitoringService>.Instance);
                var handler = new RecordObservationCommandHandler(_repository, vitalSigns);
                RecordObservationResponse result = await handler.HandleAsync(cmd, cancellationToken);
                return (TResponse)(object)result;
            }
            throw new NotSupportedException($"Request type {request?.GetType().Name} not supported");
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Non-generic SendAsync not supported");
    }
}
