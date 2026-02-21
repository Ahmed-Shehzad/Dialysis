using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Caching;
using BuildingBlocks.ExceptionHandling;
using BuildingBlocks.Logging;
using BuildingBlocks.Tenancy;
using BuildingBlocks.Interceptors;
using BuildingBlocks.TimeSync;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.Services;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;

using Dialysis.Treatment.Infrastructure;
using Dialysis.Treatment.Infrastructure.AlarmApi;
using Dialysis.Treatment.Infrastructure.DeviceRegistration;
using Dialysis.Treatment.Infrastructure.FhirSubscription;
using Dialysis.Treatment.Infrastructure.Hl7;
using Dialysis.Treatment.Infrastructure.Persistence;

using Intercessor;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Authentication:JwtBearer:Authority"];
        opts.Audience = builder.Configuration["Authentication:JwtBearer:Audience"] ?? "api://dialysis-pdms";
        opts.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:JwtBearer:RequireHttpsMetadata", true);
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                string? token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("TreatmentRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Treatment:Read", "Treatment:Admin")))
    .AddPolicy("TreatmentWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Treatment:Write", "Treatment:Admin")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(GetTreatmentSessionQuery).Assembly);
});

string connectionString = builder.Configuration.GetConnectionString("TreatmentDb")
                          ?? "Host=localhost;Database=dialysis_treatment;Username=postgres;Password=postgres";

builder.Services.AddSingleton<Transponder.Persistence.EntityFramework.PostgreSql.Abstractions.IPostgreSqlStorageOptions>(
    _ => new Transponder.Persistence.EntityFramework.PostgreSql.PostgreSqlStorageOptions());

builder.Services.AddDbContextFactory<TreatmentDbContext>((_, ob) =>
    ob.UseNpgsql(connectionString)
      .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddSingleton<Transponder.Persistence.EntityFramework.Abstractions.IEntityFrameworkDbContextFactory<TreatmentDbContext>,
    Transponder.Persistence.EntityFramework.EntityFrameworkDbContextFactory<TreatmentDbContext>>();
builder.Services.AddSingleton<Transponder.Persistence.Abstractions.IStorageSessionFactory,
    Transponder.Persistence.EntityFramework.EntityFrameworkStorageSessionFactory<TreatmentDbContext>>();
builder.Services.AddSingleton<Transponder.Persistence.Abstractions.IScheduledMessageStore,
    Transponder.Persistence.EntityFramework.EntityFrameworkScheduledMessageStore<TreatmentDbContext>>();

var treatmentBusAddress = new Uri("transponder://treatment");
builder.Services.AddTransponder(treatmentBusAddress, opts =>
{
    _ = opts.TransportBuilder.UseSignalR(treatmentBusAddress, [new Uri("signalr://treatment")]);
    string? asbConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(asbConnectionString))
        _ = opts.TransportBuilder.UseAzureServiceBus(_ => new AzureServiceBusHostSettings(treatmentBusAddress, connectionString: asbConnectionString));
    _ = opts.UseOutbox();
    _ = opts.UsePersistedMessageScheduler();
});

builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddScoped<IntegrationEventDispatchInterceptor>();
builder.Services.AddDbContext<TreatmentDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(
         sp.GetRequiredService<DomainEventDispatcherInterceptor>(),
         sp.GetRequiredService<IntegrationEventDispatchInterceptor>()));
builder.Services.AddDbContext<TreatmentReadDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddScoped<ITreatmentSessionRepository, TreatmentSessionRepository>();
builder.Services.AddScoped<TreatmentReadStore>();
string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    _ = builder.Services.AddTransponderRedisCache(opts => opts.ConnectionString = redisConnectionString);
    _ = builder.Services.AddReadThroughCache();
}
else _ = builder.Services.AddNullReadThroughCache();
builder.Services.AddScoped<ITreatmentReadStore>(sp => new CachedTreatmentReadStore(
    sp.GetRequiredService<TreatmentReadStore>(),
    sp.GetRequiredService<IReadThroughCache>()));
builder.Services.AddScoped<IOruMessageParser, OruR01Parser>();
builder.Services.AddDeviceRegistrationClient(builder.Configuration);
builder.Services.AddAlarmApiClient(builder.Configuration);
builder.Services.AddSingleton<IHl7BatchParser, Hl7BatchParser>();
builder.Services.AddSingleton<IAckR01Builder, AckR01Builder>();
builder.Services.AddSingleton<VitalSignsMonitoringService>();
builder.Services.AddFhirAuditRecorder();
builder.Services.AddTenantResolution();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddFhirSubscriptionNotifyClient(builder.Configuration);

builder.Services.Configure<TimeSyncOptions>(builder.Configuration.GetSection(TimeSyncOptions.SectionName));
IHealthChecksBuilder treatmentHealthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "treatment-db");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    _ = treatmentHealthChecks.AddRedis(redisConnectionString, name: "redis");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    var options = new DbContextOptionsBuilder<TreatmentDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using var db = new TreatmentDbContext(options);
    await db.Database.MigrateAsync();
}

app.UseCentralExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapHub<TransponderSignalRHub>("/transponder/transport").RequireAuthorization("TreatmentRead");
app.MapControllers();

await app.RunAsync();
