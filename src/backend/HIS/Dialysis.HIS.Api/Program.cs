using System.Threading.RateLimiting;
using Dialysis.BuildingBlocks.Fhir.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIS.Composition;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices;
using Dialysis.HIS.Integration;
using Dialysis.HIS.Operations;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.RaCapabilities;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

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
var fhirBulkDataExportScope = builder.Configuration["His:Fhir:BulkData:RequireScope"]
    ?? (enableFhirSmartOnFhir ? "system/*.read" : null);
var fhirSubscriptionsScope = builder.Configuration["His:Fhir:Subscriptions:RequireScope"]
    ?? (enableFhirSmartOnFhir ? "user/*.write" : null);

builder.Services.AddHospitalInformationSystem(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "his")),
    enableOutboxRelay: enableOutbox,
    enableFhirEndpoints: enableFhirEndpoints,
    enableFhirBulkDataPersistence: enableFhirBulkDataExport,
    enableFhirBulkDataExport: enableFhirBulkDataExport,
    enableFhirSmartOnFhir: enableFhirSmartOnFhir,
    enableFhirSubscriptions: enableFhirSubscriptions,
    enableFhirSubscriptionsPersistence: enableFhirSubscriptions,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        }));

builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("DeviceIngest", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

builder.Services.AddControllers();

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
app.MapControllers();

if (enableFhirEndpoints)
    app.MapFhirEndpoints();

if (enableFhirSmartOnFhir)
    app.MapSmartConfigurationEndpoint();

if (enableFhirBulkDataExport)
    app.MapFhirBulkDataEndpoints(requireScope: fhirBulkDataExportScope);

if (enableFhirSubscriptions)
    app.MapFhirSubscriptionEndpoints(requireScope: fhirSubscriptionsScope);

await app.RunAsync().ConfigureAwait(false);
