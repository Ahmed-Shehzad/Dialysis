using Dialysis.BuildingBlocks.Fhir.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIE.Composition;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Persistence;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

const string connectionStringName = "Hie";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Hie:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Hie:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Hie:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Hie:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<HiePermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "hie",
    HandlerAssemblies =
    [
        typeof(Dialysis.HIE.Outbound.HieOutboundMarker).Assembly,
        typeof(Dialysis.HIE.Inbound.HieInboundMarker).Assembly,
        typeof(Dialysis.HIE.Consent.HieConsentMarker).Assembly,
        typeof(Dialysis.HIE.OpenEhr.HieOpenEhrMarker).Assembly,
        typeof(Dialysis.HIE.Documents.HieDocumentsMarker).Assembly,
        typeof(Dialysis.HIE.Tefca.HieTefcaMarker).Assembly,
    ],
});

var enableHieDemoSeed = builder.Configuration.GetValue("Hie:Demo:Enabled", false);

builder.Services.AddHealthInformationExchange(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "hie")),
    enableOutboxRelay: enableOutbox,
    enableDemoSeed: enableHieDemoSeed,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        }));

// HIPAA Security Rule scaffolding — see src/backend/HIS/README.md for the rationale.
builder.Services.AddFhirAudit();
builder.Services.AddHipaaCompliance("hie");
builder.Services.AddHipaaAspNetCoreSafeguards();

builder.Services
    .AddControllers()
    // The SPA posts enums by name (e.g. SignDocumentRequest.CertificateSource = "Platform").
    // Without this converter System.Text.Json only binds the integer backing values, so a
    // string enum payload fails model binding and the action returns 400 before it runs.
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();
app.MapGet(
        "/health/ready",
        async (HieDbContext db, CancellationToken cancellationToken) =>
            await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
                ? Results.Text("OK", "text/plain", statusCode: StatusCodes.Status200OK)
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { module = "hie", version = "v1" }));
app.MapHipaaSafeguardsEndpoint();
// FHIR terminology operations ($validate-code / $translate / $expand / $lookup) + governance catalog,
// served under api/v1.0/fhir to sit alongside the FhirController's spec-compliant surface.
app.MapFhirTerminologyEndpoints("/api/v1.0/fhir");
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
