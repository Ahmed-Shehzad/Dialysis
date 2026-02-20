using BuildingBlocks;
using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Logging;
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
using Dialysis.Alarm.Infrastructure.Persistence;

using Intercessor;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

using Transponder;
using Serilog;
using Transponder.Transports.SignalR;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.With<ActivityEnricher>()
        .Enrich.FromLogContext());

builder.Services.AddAlarmJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AlarmRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Alarm:Read", "Alarm:Admin")))
    .AddPolicy("AlarmWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Alarm:Write", "Alarm:Admin")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddTransponder(new Uri("transponder://alarm"), opts =>
    opts.TransportBuilder.UseSignalR(new Uri("signalr://alarm")));

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(IngestOruR40MessageCommand).Assembly);
});

string connectionString = builder.Configuration.GetConnectionString("AlarmDb")
                          ?? "Host=localhost;Database=dialysis_alarm;Username=postgres;Password=postgres";

builder.Services.AddIntegrationEventBuffer();
builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddScoped<IntegrationEventOutboxInterceptor>();
builder.Services.AddDbContext<AlarmDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(
         sp.GetRequiredService<DomainEventDispatcherInterceptor>(),
         sp.GetRequiredService<IntegrationEventOutboxInterceptor>()));
builder.Services.AddIntegrationEventOutboxPublisher<AlarmDbContext>();
builder.Services.AddDbContext<AlarmReadDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<IAlarmReadStore, AlarmReadStore>();
builder.Services.AddScoped<IOruR40MessageParser, OruR40Parser>();
builder.Services.AddDeviceRegistrationClient(builder.Configuration);
builder.Services.AddSingleton<IOraR41Builder, OraR41Builder>();
builder.Services.AddSingleton<AlarmEscalationService>();
builder.Services.AddAuditRecorder();
builder.Services.AddTenantResolution();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.Configure<TimeSyncOptions>(builder.Configuration.GetSection(TimeSyncOptions.SectionName));
builder.Services.AddFhirSubscriptionNotifyClient(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "alarm-db");

WebApplication app = builder.Build();

app.UseTenantResolution();
await app.ApplyMigrationsIfDevelopmentAsync();
app.UseAlarmExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapHub<TransponderSignalRHub>("/transponder/transport").RequireAuthorization("AlarmRead");
app.MapControllers();

await app.RunAsync();
