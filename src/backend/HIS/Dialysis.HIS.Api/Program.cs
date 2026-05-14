using System.Threading.RateLimiting;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIS.Composition;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices;
using Dialysis.HIS.Integration;
using Dialysis.HIS.Operations;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.RaCapabilities;
using Dialysis.Module.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddHospitalInformationSystem(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "his")),
    enableOutboxRelay: enableOutbox,
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

await app.RunAsync().ConfigureAwait(false);
