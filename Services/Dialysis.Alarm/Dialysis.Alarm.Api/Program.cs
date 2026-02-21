using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Caching;
using BuildingBlocks.ExceptionHandling;
using BuildingBlocks.Logging;
using BuildingBlocks.Options;
using BuildingBlocks.Tenancy;
using BuildingBlocks.Interceptors;
using BuildingBlocks.TimeSync;

using Dialysis.Alarm.Api;
using Dialysis.Alarm.Application.Abstractions;
using Dialysis.Alarm.Application.Domain.Services;
using Dialysis.Alarm.Application.Features.IngestOruR40Message;

using Dialysis.Alarm.Infrastructure;
using Dialysis.Alarm.Infrastructure.DeviceRegistration;
using Dialysis.Alarm.Infrastructure.FhirSubscription;
using Dialysis.Alarm.Infrastructure.Hl7;
using Dialysis.Alarm.Infrastructure.IntegrationEvents;
using Dialysis.Alarm.Infrastructure.Persistence;

using Intercessor;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

using Azure.Identity;

using Transponder;
using Transponder.Persistence.Redis;
using Transponder.Transports.AzureServiceBus;
using Transponder.Transports.SignalR;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.With<ActivityEnricher>()
        .Enrich.FromLogContext());

builder.Services.AddJwtBearerStartupValidation(builder.Configuration);
builder.Services.AddAlarmJwtAuthentication(builder.Configuration);
builder.Services.AddCentralExceptionHandler(builder.Configuration);
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AlarmRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Alarm:Read", "Alarm:Admin")))
    .AddPolicy("AlarmWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Alarm:Write", "Alarm:Admin")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
Microsoft.AspNetCore.SignalR.ISignalRServerBuilder signalrBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    _ = signalrBuilder.AddStackExchangeRedis(redisConnectionString, o => o.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("Alarm"));

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(IngestOruR40MessageCommand).Assembly);
});

string connectionString = builder.Configuration.GetConnectionString("AlarmDb")
                          ?? "Host=localhost;Database=dialysis_alarm;Username=postgres;Password=postgres";

builder.Services.AddSingleton<Transponder.Persistence.EntityFramework.PostgreSql.Abstractions.IPostgreSqlStorageOptions>(
    _ => new Transponder.Persistence.EntityFramework.PostgreSql.PostgreSqlStorageOptions());

// Custom factory for Transponder (outbox, scheduler) to avoid AddDbContext + AddDbContextFactory conflict
// that causes "Cannot resolve scoped service from root provider" when both register the same DbContext type.
builder.Services.AddSingleton<Transponder.Persistence.EntityFramework.Abstractions.IEntityFrameworkDbContextFactory<AlarmDbContext>>(
    _ => new TransponderAlarmDbContextFactory(connectionString));
builder.Services.AddSingleton<Transponder.Persistence.Abstractions.IStorageSessionFactory,
    Transponder.Persistence.EntityFramework.EntityFrameworkStorageSessionFactory<AlarmDbContext>>();
builder.Services.AddSingleton<Transponder.Persistence.Abstractions.IScheduledMessageStore,
    Transponder.Persistence.EntityFramework.EntityFrameworkScheduledMessageStore<AlarmDbContext>>();

var alarmBusAddress = new Uri("transponder://alarm");
string? asbConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
string? asbNamespace = builder.Configuration["AzureServiceBus:FullyQualifiedNamespace"];
bool asbConfigured = !string.IsNullOrWhiteSpace(asbConnectionString) || !string.IsNullOrWhiteSpace(asbNamespace);

builder.Services.AddTransponder(alarmBusAddress, opts =>
{
    _ = opts.TransportBuilder.UseSignalR(alarmBusAddress, [new Uri("signalr://alarm")]);
    if (!string.IsNullOrWhiteSpace(asbConnectionString))
        _ = opts.TransportBuilder.UseAzureServiceBus(_ => new AzureServiceBusHostSettings(alarmBusAddress, connectionString: asbConnectionString));
    else if (!string.IsNullOrWhiteSpace(asbNamespace))
        _ = opts.TransportBuilder.UseAzureServiceBus(_ => new AzureServiceBusHostSettings(alarmBusAddress, fullyQualifiedNamespace: asbNamespace, credential: new DefaultAzureCredential()));
    _ = opts.UseOutbox();
    _ = opts.UsePersistedMessageScheduler();
});
if (asbConfigured)
    _ = builder.Services.AddThresholdBreachDetectedReceiveEndpoint(alarmBusAddress);

builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddScoped<IntegrationEventDispatchInterceptor>();
builder.Services.AddDbContext<AlarmDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(
         sp.GetRequiredService<DomainEventDispatcherInterceptor>(),
         sp.GetRequiredService<IntegrationEventDispatchInterceptor>()));
builder.Services.AddDbContext<AlarmReadDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<IEscalationIncidentStore, EscalationIncidentStore>();
builder.Services.AddScoped<AlarmReadStore>();
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    _ = builder.Services.AddTransponderRedisCache(opts => opts.ConnectionString = redisConnectionString);
    _ = builder.Services.AddReadThroughCache();
}
else _ = builder.Services.AddNullReadThroughCache();
builder.Services.AddScoped<IAlarmReadStore>(sp => new CachedAlarmReadStore(
    sp.GetRequiredService<AlarmReadStore>(),
    sp.GetRequiredService<IReadThroughCache>()));
builder.Services.AddScoped<IOruR40MessageParser, OruR40Parser>();
builder.Services.AddDeviceRegistrationClient(builder.Configuration);
builder.Services.AddSingleton<IOraR41Builder, OraR41Builder>();
builder.Services.AddSingleton<AlarmEscalationService>();
builder.Services.AddFhirAuditRecorder();
builder.Services.AddTenantResolution();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.Configure<TimeSyncOptions>(builder.Configuration.GetSection(TimeSyncOptions.SectionName));
builder.Services.AddFhirSubscriptionNotifyClient(builder.Configuration);

IHealthChecksBuilder alarmHealthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "alarm-db");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    _ = alarmHealthChecks.AddRedis(redisConnectionString, name: "redis");

WebApplication app = builder.Build();

app.UseTenantResolution();
await app.ApplyMigrationsIfDevelopmentAsync();
app.UseCentralExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapHub<TransponderSignalRHub>("/transponder/transport").RequireAuthorization("AlarmRead");
app.MapControllers();

if (asbConfigured)
    try
    {
        if (!string.IsNullOrWhiteSpace(asbConnectionString))
            await app.Services.EnsureThresholdBreachTopicAndSubscriptionAsync(asbConnectionString);
        else if (!string.IsNullOrWhiteSpace(asbNamespace))
            await app.Services.EnsureThresholdBreachTopicAndSubscriptionAsync(asbNamespace, new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "ASB topology provisioning failed. Ensure topic ThresholdBreachDetectedIntegrationEvent and subscription alarm-threshold-breach exist (e.g. via emulator Config.json or infra).");
    }

await app.RunAsync();

namespace Dialysis.Alarm.Api { public partial class Program { } }
