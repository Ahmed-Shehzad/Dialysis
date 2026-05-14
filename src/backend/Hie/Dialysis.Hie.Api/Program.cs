using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Hie.Composition;
using Dialysis.Hie.Contracts.Security;
using Dialysis.Hie.Persistence;
using Dialysis.Module.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
        typeof(Dialysis.Hie.Outbound.HieOutboundMarker).Assembly,
        typeof(Dialysis.Hie.Inbound.HieInboundMarker).Assembly,
        typeof(Dialysis.Hie.Consent.HieConsentMarker).Assembly,
        typeof(Dialysis.Hie.OpenEhr.HieOpenEhrMarker).Assembly,
    ],
});

builder.Services.AddHealthInformationExchange(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "hie")),
    enableOutboxRelay: enableOutbox,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        }));

builder.Services.AddControllers();

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
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
