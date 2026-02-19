using BuildingBlocks.Tenancy;

using Dialysis.Hl7ToFhir;
using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Application.Features.IngestOruMessage;
using Dialysis.Treatment.Application.Features.RecordObservation;

using Dialysis.Treatment.Infrastructure.Hl7;
using Dialysis.Treatment.Infrastructure.Persistence;
using Dialysis.Treatment.Tests.TestDoubles;

using Hl7.Fhir.Model;

using Intercessor.Abstractions;

using Task = System.Threading.Tasks.Task;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// End-to-end integration tests: parse ORU^R01 → IngestOruMessage → RecordObservation → GetTreatmentSession → FHIR Bundle.
/// </summary>
public sealed class OruR01ToFhirIntegrationTests
{
    [Fact]
    public async Task OruR01_IngestAndRecord_ProducesFhirBundleWithProcedureAndObservationsAsync()
    {
        await using TreatmentDbContext db = CreateDbContext();
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new TreatmentSessionRepository(db, tenant);
        var ingestHandler = new IngestOruMessageCommandHandler(new Sender(repository), new OruR01Parser(), new NoOpDeviceRegistrationClient());
        var getHandler = new GetTreatmentSessionQueryHandler(repository);

        const string oruR01 = """
            MSH|^~\&|MACH^EUI64^EUI-64|FAC|PDMS|FAC|20230215120000||ORU^R01^ORU_R01|MSG001|P|2.6
            PID|||MRN123^^^^MR
            OBR|1||THERAPY001^MACH^EUI64|||20230215120000||||||start
            OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM|||||F|||20230215120000|||AMEAS
            OBX|2|NM|158776^MDC_HDIALY_BLD_PUMP_PRESS_VEN^MDC|1.1.3.2|120|mmHg^mm[Hg]^UCUM|80-200||||F|||20230215120100|||AMEAS
            """;

#pragma warning disable IDE0058
        IngestOruMessageResponse ingestResponse = await ingestHandler.HandleAsync(new IngestOruMessageCommand(oruR01));
        ingestResponse.ObservationCount.ShouldBe(2);
        ingestResponse.SessionId.ShouldBe("THERAPY001");

        GetTreatmentSessionResponse? sessionResponse = await getHandler.HandleAsync(new GetTreatmentSessionQuery(new SessionId("THERAPY001")));
        sessionResponse.ShouldNotBeNull();
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
        res0.ShouldNotBeNull();
        res0.ShouldBeOfType<Procedure>();
        ((Procedure)res0!).Code!.Text.ShouldBe("Hemodialysis");

        Resource? res1 = bundle.Entry[1].Resource;
        res1.ShouldNotBeNull();
        res1.ShouldBeOfType<Observation>();
        var obs1 = (Observation)res1!;
        obs1.Code!.Coding.ShouldContain(c => c.System == "urn:iso:std:iso:11073:10101");
        obs1.Value.ShouldBeOfType<Quantity>();
        ((Quantity)(obs1.Value ?? throw new InvalidOperationException("obs1.Value is null"))).Value.ShouldBe(300);

        Resource? prov1 = bundle.Entry[2].Resource;
        prov1.ShouldNotBeNull();
        prov1.ShouldBeOfType<Provenance>();
        var provenance1 = (Provenance)prov1;
        provenance1.Target.ShouldContain(t => t.Reference == "urn:uuid:obs-152348-1.1.3.1");
        provenance1.Activity!.Text.ShouldBe("Automatic measurement");

        Resource? res2 = bundle.Entry[3].Resource;
        res2.ShouldNotBeNull();
        res2.ShouldBeOfType<Observation>();
        var obs2 = (Observation)res2;
        ((Quantity)(obs2.Value ?? throw new InvalidOperationException("obs2.Value is null"))).Value.ShouldBe(120);
        obs2.ReferenceRange.ShouldContain(r => r.Text == "80-200");

        Resource? prov2 = bundle.Entry[4].Resource;
        prov2.ShouldNotBeNull();
        prov2.ShouldBeOfType<Provenance>();
        var provenance2 = (Provenance)prov2;
        provenance2.Target.ShouldContain(t => t.Reference == "urn:uuid:obs-158776-1.1.3.2");
#pragma warning restore IDE0058
    }

    private static TreatmentDbContext CreateDbContext()
    {
        DbContextOptions<TreatmentDbContext> options = new DbContextOptionsBuilder<TreatmentDbContext>()
            .UseInMemoryDatabase("OruR01ToFhir_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new TreatmentDbContext(options);
    }

    private sealed class Sender : ISender
    {
        private readonly ITreatmentSessionRepository _repository;

        public Sender(ITreatmentSessionRepository repository) => _repository = repository;

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is RecordObservationCommand cmd)
            {
                var handler = new RecordObservationCommandHandler(_repository);
                RecordObservationResponse result = await handler.HandleAsync(cmd, cancellationToken);
                return (TResponse)(object)result;
            }
            throw new NotSupportedException($"Request type {request?.GetType().Name} not supported");
        }

        public Task SendAsync(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Non-generic SendAsync not supported");
    }
}
