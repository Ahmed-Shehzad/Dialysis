using System.Text.Json.Serialization;
using Dialysis.BuildingBlocks.Fhir.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIE.Api.Controllers;
using Dialysis.HIE.Composition;
using Dialysis.HIE.Consent;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents;
using Dialysis.HIE.Inbound;
using Dialysis.HIE.OpenEhr;
using Dialysis.HIE.Outbound;
using Dialysis.HIE.Persistence;
using Dialysis.HIE.Query;
using Dialysis.HIE.Tefca;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Api;

/// <summary>Application entry point.</summary>
public class Program
{
    /// <summary>Builds and runs the host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        const string connectionStringName = "Hie";
        var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"ConnectionStrings:{connectionStringName} must be set — this module persists to PostgreSQL.");
        var enableOutbox = builder.Configuration.GetValue("Hie:Transponder:EnableOutboxRelay", false);
        var rabbitUri = builder.Configuration["Hie:Transponder:RabbitMq:ConnectionUri"];
        var rabbitQueue = builder.Configuration["Hie:Transponder:RabbitMq:QueueName"];
        var rabbitExchange = builder.Configuration["Hie:Transponder:RabbitMq:ExchangeName"];

        builder.AddModuleHost<HiePermissionCatalog>(new ModuleHostingOptions
        {
            ModuleSlug = "hie",
            HandlerAssemblies =
            [
                typeof(HieOutboundMarker).Assembly,
                typeof(HieInboundMarker).Assembly,
                typeof(HieQueryMarker).Assembly,
                typeof(HieConsentMarker).Assembly,
                typeof(HieOpenEhrMarker).Assembly,
                typeof(HieDocumentsMarker).Assembly,
                typeof(HieTefcaMarker).Assembly,
            ],
        });

        builder.Services.AddHealthInformationExchange(
            builder.Configuration,
            configurePersistence: options => options.UseNpgsql(
                    connectionString,
                    pg => pg.MigrationsHistoryTable("__ef_migrations", "hie")),
            enableOutboxRelay: enableOutbox,
            configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
                ? null
                : s => s.AddTransponderRabbitMq(o =>
                {
                    o.ConnectionUri = rabbitUri;
                    if (!string.IsNullOrWhiteSpace(rabbitQueue))
                        o.QueueName = rabbitQueue;
                    if (!string.IsNullOrWhiteSpace(rabbitExchange))
                        o.ExchangeName = rabbitExchange;
                }));

        // HIPAA Security Rule scaffolding — see src/backend/HIS/README.md for the rationale.
        builder.Services.AddFhirAudit();
        builder.Services.AddHipaaCompliance("hie");
        builder.Services.AddHipaaAspNetCoreSafeguards();

        // Patient self-access gate (own-patient, plus dev-only staff impersonation). Scoped: depends on ICurrentUser.
        builder.Services.AddScoped<HiePortalAccess>();

        builder.Services
            .AddControllers()
            // The SPA posts enums by name (e.g. SignDocumentRequest.CertificateSource = "Platform").
            // Without this converter System.Text.Json only binds the integer backing values, so a
            // string enum payload fails model binding and the action returns 400 before it runs.
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        var app = builder.Build();

        // Apply EF migrations on startup so the HIE module is self-hosting against a fresh
        // Aspire-managed Postgres container. Gated on configuration (Hie:AutoMigrate, default
        // true in Development) — in production the DBA owns the migration step out-of-band.
        // (Previously the demo seeder applied migrations as a side effect; that was removed in
        // #167, so the schema must be created here like HIS does.)
        if (!string.IsNullOrWhiteSpace(connectionString)
            && builder.Configuration.GetValue("Hie:AutoMigrate", app.Environment.IsDevelopment()))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
            await db.Database.MigrateAsync().ConfigureAwait(false);
        }

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
    }
}
