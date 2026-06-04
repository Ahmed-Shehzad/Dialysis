using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Api.Demo;

/// <summary>
/// Development-only seeder for the SPA Integrations console. Creates a catalogue of grouped
/// integration flows that mirror the real hub use-cases (HL7 v2 inbound feeds, v2 → FHIR bridging,
/// FHIR/HIE outbound, clinical notifications, ETL, and legacy MLLP/file drops) so the operator
/// view is populated without external clients. The two simulator-driven flows
/// (<see cref="DemoAdtFlowId"/> / <see cref="DemoOruFlowId"/>) keep their well-known ids because
/// <see cref="Hl7V2SimulatorService"/> dispatches into them by id. Idempotent.
/// </summary>
public sealed class SmartConnectDemoSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SmartConnectDemoSeeder> _logger;
    /// <summary>
    /// Development-only seeder for the SPA Integrations console. Creates a catalogue of grouped
    /// integration flows that mirror the real hub use-cases (HL7 v2 inbound feeds, v2 → FHIR bridging,
    /// FHIR/HIE outbound, clinical notifications, ETL, and legacy MLLP/file drops) so the operator
    /// view is populated without external clients. The two simulator-driven flows
    /// (<see cref="DemoAdtFlowId"/> / <see cref="DemoOruFlowId"/>) keep their well-known ids because
    /// <see cref="Hl7V2SimulatorService"/> dispatches into them by id. Idempotent.
    /// </summary>
    public SmartConnectDemoSeeder(IServiceProvider services, ILogger<SmartConnectDemoSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    // Simulator-driven flows — ids are a contract with Hl7V2SimulatorService, do not change.
    public static readonly Guid DemoAdtFlowId = new("9d2b1a00-0000-0000-0000-00000000ad01");
    public static readonly Guid DemoOruFlowId = new("9d2b1a00-0000-0000-0000-00000000ad02");

    // Channel groups (Mirth-style organisational rail).
    private static readonly Guid _groupInbound = new("9d2b1a00-0000-0000-0000-0000000060a1");
    private static readonly Guid _groupFhir = new("9d2b1a00-0000-0000-0000-0000000060a2");
    private static readonly Guid _groupNotify = new("9d2b1a00-0000-0000-0000-0000000060a3");
    private static readonly Guid _groupEtl = new("9d2b1a00-0000-0000-0000-0000000060a4");
    private static readonly Guid _groupLegacy = new("9d2b1a00-0000-0000-0000-0000000060a5");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IIntegrationFlowRepository>();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        await EnsureGroupsAsync(db, cancellationToken).ConfigureAwait(false);

        var seeded = 0;
        foreach (var flow in BuildCatalogue())
        {
            if (await repo.GetByIdAsync(flow.Id, cancellationToken).ConfigureAwait(false) is not null)
                continue;
            await repo.AddAsync(flow, cancellationToken).ConfigureAwait(false);
            seeded++;
        }

        _logger.LogInformation(
            "SmartConnect demo seeder: ensured 5 channel groups and {Seeded} new integration flows.",
            seeded);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureGroupsAsync(SmartConnectDbContext db, CancellationToken ct)
    {
        (Guid Id, string Name, string Description)[] groups =
        [
            (_groupInbound, "Inbound HL7 v2 Feeds", "MLLP/HTTP listeners accepting ADT, ORU, ORM, SIU, VXU and MDM trigger events from hospital systems."),
            (_groupFhir, "FHIR Exchange", "HL7 v2 → FHIR R4 bridging and outbound FHIR Bundle delivery to the regional HIE / partner endpoints."),
            (_groupNotify, "Clinical Notifications", "Threshold-driven alerting: critical labs, adverse events and discharge notifications to care teams."),
            (_groupEtl, "ETL & Persistence", "Scheduled extracts and message archival into the analytics warehouse and cold storage."),
            (_groupLegacy, "Legacy & File Drops", "Legacy MLLP partners and filesystem-based batch interfaces kept stopped until cut-over."),
        ];

        foreach (var (id, name, description) in groups)
        {
            if (await db.FlowGroups.AnyAsync(g => g.Id == id, ct).ConfigureAwait(false))
                continue;
            db.FlowGroups.Add(new FlowGroupEntity { Id = id, Name = name, Description = description });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static IEnumerable<IntegrationFlow> BuildCatalogue()
    {
        // ---- Inbound HL7 v2 Feeds ------------------------------------------------------------
        yield return Flow(
            DemoAdtFlowId, "Demo ADT^A01 inbound (HL7 v2)",
            "Patient admit/transfer/discharge feed. Driven by the in-process HL7 v2 simulator.",
            FlowRuntimeState.Started, _groupInbound, ["demo", "hl7v2", "adt"],
            Pipeline(
                filters: [RuleContains("ADT^")],
                outbound: [Route("pass-through")]));

        yield return Flow(
            DemoOruFlowId, "Demo ORU^R01 inbound (HL7 v2)",
            "Unsolicited lab observation results. Driven by the in-process HL7 v2 simulator.",
            FlowRuntimeState.Started, _groupInbound, ["demo", "hl7v2", "oru", "lab"],
            Pipeline(
                filters: [RuleContains("ORU^")],
                outbound: [Route("pass-through")]));

        yield return Flow(
            Id("ad03"), "SIU^S12 scheduling inbound (HL7 v2)",
            "Appointment booking notifications from the scheduling system, normalised to the flow ledger.",
            FlowRuntimeState.Started, _groupInbound, ["hl7v2", "siu", "scheduling"],
            Pipeline(filters: [RuleContains("SIU^")], outbound: [Route("pass-through")]));

        yield return Flow(
            Id("ad04"), "ORM^O01 order inbound (HL7 v2)",
            "Lab and imaging order messages accepted from the order-entry system.",
            FlowRuntimeState.Started, _groupInbound, ["hl7v2", "orm", "orders"],
            Pipeline(filters: [RuleContains("ORM^")], outbound: [Route("pass-through")]));

        yield return Flow(
            Id("ad05"), "VXU^V04 immunization inbound (HL7 v2)",
            "Immunization history submissions. Paused pending registry credential rotation.",
            FlowRuntimeState.Paused, _groupInbound, ["hl7v2", "vxu", "immunization"],
            Pipeline(filters: [RuleContains("VXU^")], outbound: [Route("pass-through")]));

        // ---- FHIR Exchange -------------------------------------------------------------------
        yield return Flow(
            Id("fb01"), "ADT → FHIR Patient bridge",
            "Maps admit events to a FHIR R4 Patient and POSTs it to the HIS FHIR facade.",
            FlowRuntimeState.Started, _groupFhir, ["fhir", "bridge", "patient"],
            Pipeline(
                filters: [RuleContains("ADT^A01")],
                transforms: [Js("// map ADT PID → FHIR Patient\nreturn payloadText;")],
                outbound:
                [
                    Route("http", """{"Url":"http://localhost:9090/fhir/his/Patient","Method":"POST","Headers":{"Content-Type":"application/fhir+json"}}""", maxAttempts: 3),
                ]));

        yield return Flow(
            Id("fb02"), "ORU → FHIR Observation bridge",
            "Maps lab OBX segments to FHIR Observations and POSTs them to the EHR FHIR facade.",
            FlowRuntimeState.Started, _groupFhir, ["fhir", "bridge", "observation", "lab"],
            Pipeline(
                filters: [RuleContains("ORU^R01")],
                transforms: [Js("// map OBX → FHIR Observation\nreturn payloadText;")],
                outbound:
                [
                    Route("http", """{"Url":"http://localhost:9090/fhir/ehr/Observation","Method":"POST","Headers":{"Content-Type":"application/fhir+json"}}""", maxAttempts: 3),
                ]));

        yield return Flow(
            Id("fb03"), "Outbound FHIR Bundle → Regional HIE",
            "Forwards a transaction Bundle to the regional Health Information Exchange partner endpoint.",
            FlowRuntimeState.Started, _groupFhir, ["fhir", "outbound", "hie", "partner"],
            Pipeline(
                filters: [AllowAll()],
                outbound:
                [
                    Route("http", """{"Url":"https://hie.example.org/fhir","Method":"POST","Headers":{"Content-Type":"application/fhir+json","Accept":"application/fhir+json"}}""", maxAttempts: 5),
                ]));

        yield return Flow(
            Id("fb04"), "FHIR Subscription fan-out → care team",
            "Re-dispatches subscription-notification Bundles to the care-team WebSocket/SSE delivery flow.",
            FlowRuntimeState.Started, _groupFhir, ["fhir", "subscriptions", "real-time"],
            Pipeline(
                filters: [AllowAll()],
                outbound: [Route("channel-writer", $$"""{"TargetFlowId":"{{Id("nt01")}}","PreserveCorrelationId":true}""")]));

        // ---- Clinical Notifications ----------------------------------------------------------
        yield return Flow(
            Id("nt01"), "Critical lab result → on-call email",
            "Rule-builder threshold on potassium/creatinine OBX values; pages the on-call nephrologist by email.",
            FlowRuntimeState.Started, _groupNotify, ["alerting", "lab", "email"],
            Pipeline(
                filters: [RuleContains("OBX|")],
                outbound:
                [
                    Route("smtp", """{"Host":"smtp.hospital.local","Port":25,"From":"alerts@hospital.local","To":"oncall-neph@hospital.local","Subject":"Critical lab result"}"""),
                ]));

        yield return Flow(
            Id("nt02"), "Adverse event → care-team SMS gateway",
            "Intradialytic adverse events posted to the SMS gateway webhook for immediate care-team paging.",
            FlowRuntimeState.Started, _groupNotify, ["alerting", "adverse-event", "sms"],
            Pipeline(
                filters: [AllowAll()],
                outbound:
                [
                    Route("http", """{"Url":"https://sms-gateway.example.org/send","Method":"POST","Headers":{"Content-Type":"application/json"}}""", maxAttempts: 3),
                ]));

        yield return Flow(
            Id("nt03"), "ADT discharge → bed-management webhook",
            "Notifies the bed-management board on A03 discharge. Paused while the board API is upgraded.",
            FlowRuntimeState.Paused, _groupNotify, ["adt", "discharge", "webhook"],
            Pipeline(
                filters: [RuleContains("ADT^A03")],
                outbound:
                [
                    Route("http", """{"Url":"https://beds.example.org/events","Method":"POST"}"""),
                ]));

        // ---- ETL & Persistence ---------------------------------------------------------------
        yield return Flow(
            Id("et01"), "Nightly census → analytics warehouse",
            "Scheduled census extract written to the analytics Postgres warehouse. Paused outside the nightly window.",
            FlowRuntimeState.Paused, _groupEtl, ["etl", "analytics", "database"],
            Pipeline(
                filters: [AllowAll()],
                outbound:
                [
                    Route("database", """{"Provider":1,"ConnectionStringName":"Analytics","Sql":"INSERT INTO census_raw(correlation_id, payload, received_at) VALUES (@cid, @body, @ts)","Parameters":[{"Name":"cid","Source":3},{"Name":"body","Source":1},{"Name":"ts","Source":6}]}"""),
                ]));

        yield return Flow(
            Id("et02"), "Message archive → cold storage",
            "Appends every accepted message to the daily NDJSON archive on the cold-storage volume.",
            FlowRuntimeState.Started, _groupEtl, ["archive", "compliance", "file"],
            Pipeline(
                filters: [AllowAll()],
                outbound:
                [
                    Route("file", """{"Path":"/var/smartconnect/archive/messages.ndjson","Append":true}"""),
                ]));

        // ---- Legacy & File Drops -------------------------------------------------------------
        yield return Flow(
            Id("lg01"), "Legacy A19 query → MLLP partner",
            "Patient query (QRY^A19) forwarded over MLLP to the legacy registry. Stopped until cut-over.",
            FlowRuntimeState.Stopped, _groupLegacy, ["legacy", "mllp", "tcp"],
            Pipeline(
                filters: [RuleContains("QRY^A19")],
                outbound:
                [
                    Route("tcp", """{"Host":"legacy-registry.example.org","Port":2575,"Framing":2,"ConnectTimeoutMs":5000}"""),
                ]));

        yield return Flow(
            Id("lg02"), "Lab CSV drop → folder ingest",
            "Watches a partner SFTP landing folder for batch lab CSVs. Stopped pending partner go-live.",
            FlowRuntimeState.Stopped, _groupLegacy, ["legacy", "batch", "file"],
            Pipeline(
                filters: [AllowAll()],
                outbound: [Route("file", """{"Path":"/var/smartconnect/ingest/processed.log","Append":true}""")]));
    }

    // ---- builders ----------------------------------------------------------------------------

    // Deterministic GUID from a mnemonic label so the catalogue is stable + idempotent across
    // restarts without constraining labels to hex (e.g. "nt01", "lg02" are not valid GUID chars).
    private static Guid Id(string suffix) => new(
        System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"dialysis.smartconnect.demo-flow:{suffix}")));

    private static IntegrationFlow Flow(
        Guid id, string name, string description, FlowRuntimeState state,
        Guid groupId, List<string> tags, IntegrationFlowPipelineDefinition pipeline) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            RuntimeState = state,
            GroupId = groupId,
            Tags = tags,
            Pipeline = pipeline,
        };

    private static IntegrationFlowPipelineDefinition Pipeline(
        List<RouteFilterSlot>? filters = null,
        List<TransformStageSlot>? transforms = null,
        List<OutboundRouteSlot>? outbound = null) =>
        new()
        {
            RouteFilters = filters ?? [],
            SourceTransformStages = transforms ?? [],
            OutboundRoutes = outbound ?? [],
        };

    private static RouteFilterSlot RuleContains(string needle) => new()
    {
        Kind = "rule-builder",
        ParametersJson = $$"""{"match":"all","rules":[{"type":"payloadContains","value":"{{needle}}"}]}""",
    };

    private static RouteFilterSlot AllowAll() => new() { Kind = "allow-all" };

    private static TransformStageSlot Js(string script) => new()
    {
        Kind = "javascript",
        ParametersJson = System.Text.Json.JsonSerializer.Serialize(new { script }),
    };

    private static OutboundRouteSlot Route(string kind, string? parametersJson = null, int maxAttempts = 1) => new()
    {
        OutboundAdapterKind = kind,
        OutboundParametersJson = parametersJson,
        MaxAttempts = maxAttempts,
    };
}
