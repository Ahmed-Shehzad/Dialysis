using System.Threading.RateLimiting;
using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.BuildingBlocks.DurableCommandBus.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Hipaa;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIS.Composition;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices;
using Dialysis.HIS.Integration;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Dialysis.HIS.Operations;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.RaCapabilities;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Api;

/// <summary>Application entry point.</summary>
public partial class Program
{
    /// <summary>Builds and runs the host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        if (builder.Configuration.GetValue("His:UseForwardedHeaders", false))
        {
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
            });
        }

        const string connectionStringName = "His";
        var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"ConnectionStrings:{connectionStringName} must be set — this module persists to PostgreSQL.");
        var enableOutbox = builder.Configuration.GetValue("His:Transponder:EnableOutboxRelay", false);
        var rabbitUri = builder.Configuration["His:Transponder:RabbitMq:ConnectionUri"];
        var rabbitQueue = builder.Configuration["His:Transponder:RabbitMq:QueueName"];
        var rabbitExchange = builder.Configuration["His:Transponder:RabbitMq:ExchangeName"];

        builder.AddModuleHost<HisPermissionCatalog>(new ModuleHostingOptions
        {
            ModuleSlug = "his",
            HandlerAssemblies =
            [
                typeof(HisOperationsMarker).Assembly,
                typeof(HisDataServicesMarker).Assembly,
                typeof(HisIntegrationMarker).Assembly,
                typeof(RaCapabilitiesMarker).Assembly
            ],
        });

        var enableFhirEndpoints = builder.Configuration.GetValue("His:Fhir:Enabled", false);
        var enableFhirBulkDataExport = builder.Configuration.GetValue("His:Fhir:BulkData:Enabled", false);
        var enableFhirSmartOnFhir = builder.Configuration.GetValue("His:Fhir:Smart:Enabled", false);
        var enableFhirSubscriptions = builder.Configuration.GetValue("His:Fhir:Subscriptions:Enabled", false);
        // Persistence defaults to the feature flag; set false to use the in-memory registry
        // (no DB / migration dependency — handy for local dev and demos).
        var enableFhirSubscriptionsPersistence =
            builder.Configuration.GetValue("His:Fhir:Subscriptions:Persistence", enableFhirSubscriptions);
        var fhirBulkDataExportScope = builder.Configuration["His:Fhir:BulkData:RequireScope"]
            ?? (enableFhirSmartOnFhir ? "system/*.read" : null);
        var fhirSubscriptionsScope = builder.Configuration["His:Fhir:Subscriptions:RequireScope"]
            ?? (enableFhirSmartOnFhir ? "user/*.write" : null);

        builder.Services.AddHospitalInformationSystem(
            builder.Configuration,
            configurePersistence: options => options.UseNpgsql(
                    connectionString,
                    pg => pg.MigrationsHistoryTable("__ef_migrations", "his")),
            enableOutboxRelay: enableOutbox,
            enableFhirEndpoints: enableFhirEndpoints,
            enableFhirBulkDataPersistence: enableFhirBulkDataExport,
            enableFhirBulkDataExport: enableFhirBulkDataExport,
            enableFhirSmartOnFhir: enableFhirSmartOnFhir,
            enableFhirSubscriptions: enableFhirSubscriptions,
            enableFhirSubscriptionsPersistence: enableFhirSubscriptionsPersistence,
            configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
                ? null
                : s => s.AddTransponderRabbitMq(o =>
                {
                    o.ConnectionUri = rabbitUri;
                    if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
                    if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
                }));

        // HIPAA Security Rule scaffolding — PHI column encryption, PHI-access audit pipeline,
        // compliance dashboard. AddFhirAudit() supplies the IAuditEventEmitter the pipeline behaviour
        // needs; AddHipaaAspNetCoreSafeguards() adds the HSTS check that lives in the ASP.NET sibling
        // project. The /admin/hipaa/safeguards endpoint is mapped below.
        // The EF-backed audit store is wired unconditionally so PHI-access audits survive a host restart
        // — the previous conditional (FHIR-endpoints gated) left audits in memory when FHIR was off.
        builder.Services.AddFhirAudit();
        builder.Services.AddFhirAuditEntityFrameworkStore<HisDbContext>();
        builder.Services.AddHipaaCompliance("his");
        builder.Services.AddHipaaAspNetCoreSafeguards();

        // Coarse edge abuse-guard for the device-reading ingest endpoint. This is NOT a per-device cap:
        // the partition callback runs before model binding (no body `deviceId`) and before authentication
        // (no principal), and every reading arrives through the BFF + gateway so the connection IP is a
        // single upstream host. A per-device-sized cap here would throttle the whole fleet in aggregate —
        // per-device fairness is owned by the in-process SlidingWindowRateLimiter (keyed on DeviceId in
        // IngestDeviceReadingCommandHandler). Limits are configurable so ops can size the flood guard per
        // environment; the default is a fleet-sane ~200/s. Rejection is 429 + Retry-After (set globally by
        // the module-default RateLimiterOptions / ModuleRateLimitingExtensions.OnRejected).
        builder.Services.Configure<DeviceIngestRateLimitOptions>(
            builder.Configuration.GetSection(DeviceIngestRateLimitOptions.SectionName));

        builder.Services.AddRateLimiter(o =>
        {
            o.AddPolicy("DeviceIngest", context =>
            {
                var limits = context.RequestServices
                    .GetRequiredService<IOptions<DeviceIngestRateLimitOptions>>().Value;
                if (!limits.Enabled)
                    return RateLimitPartition.GetNoLimiter("device-ingest-disabled");

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = limits.QueueLimit,
                    });
            });
        });

        // Durable command bus. Optional — flips on per-command via the feature flag; with the
        // flag off, controllers stay on the existing synchronous ICqrsGateway path. Follows the
        // PDMS opt-in pattern from PR #140; second slice on the durable path.
        builder.Services.AddDurableCommandBus<HisDbContext>("his", b =>
        {
            b.RegisterCommand<IngestDeviceReadingCommand, Guid>(requiredPermission: HisPermissions.DeviceIngest);
        });
        // Surface the bus's meter to the OTLP pipeline so the Aspire dashboard + the prod
        // Grafana dashboards (deploy/k8s/observability/dashboards/) pick up its counters +
        // histograms automatically.
        builder.Services.Configure<Dialysis.Module.Hosting.Telemetry.ModuleTelemetryOptions>(o =>
            o.AdditionalMeters.Add(DurableCommandMetrics.MeterName));

        builder.Services
            .AddControllers()
            // Accept string-named enum payloads from the SPA (consistent with PDMS/SmartConnect);
            // otherwise System.Text.Json only binds the integer backing values and rejects names with 400.
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

        var app = builder.Build();

        // Apply EF migrations on startup so the HIS module is self-hosting against a fresh
        // Aspire-managed Postgres container. Gated on configuration (His:AutoMigrate, default
        // true in Development) — in production the DBA owns the migration step out-of-band.
        if (!string.IsNullOrWhiteSpace(connectionString)
            && builder.Configuration.GetValue("His:AutoMigrate", app.Environment.IsDevelopment()))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
            await db.Database.MigrateAsync().ConfigureAwait(false);
        }

        if (builder.Configuration.GetValue("His:UseForwardedHeaders", false))
            app.UseForwardedHeaders();

        if (builder.Configuration.GetValue("His:RequireHttpsRedirection", false) && !app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        if (builder.Configuration.GetValue("His:UseHsts", false) && !app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseModuleHost();
        if (enableFhirSubscriptions)
            app.UseWebSockets();
        app.UseRateLimiter();
        app.MapOpenApi();
        app.MapGet(
                "/health/ready",
                async (HisDbContext db, CancellationToken cancellationToken) =>
                    await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
                        ? Results.Text("OK", "text/plain", statusCode: StatusCodes.Status200OK)
                        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
            .AllowAnonymous();
        app.MapGet("/", () => Results.Ok(new { module = "his", version = "v1" }));
        app.MapHipaaSafeguardsEndpoint();
        app.MapDurableCommandStatusEndpoint();
        app.MapControllers();

        if (enableFhirEndpoints)
        {
            app.MapFhirEndpoints();
            app.MapFhirAuthoringEndpoints();
        }

        if (enableFhirSmartOnFhir)
            app.MapSmartConfigurationEndpoint();

        if (enableFhirBulkDataExport)
            app.MapFhirBulkDataEndpoints(requireScope: fhirBulkDataExportScope);

        if (enableFhirSubscriptions)
            app.MapFhirSubscriptionEndpoints(requireScope: fhirSubscriptionsScope);

        await app.RunAsync().ConfigureAwait(false);
    }
}
