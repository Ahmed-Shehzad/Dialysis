using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Caching;
using BuildingBlocks.ExceptionHandling;
using BuildingBlocks.Interceptors;
using BuildingBlocks.Logging;
using BuildingBlocks.Options;
using BuildingBlocks.Tenancy;
using BuildingBlocks.TimeSync;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;
using Dialysis.Prescription.Application.Options;
using Dialysis.Prescription.Infrastructure;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Intercessor;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.Redis;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.With<ActivityEnricher>()
        .Enrich.FromLogContext());

builder.Services.AddJwtBearerStartupValidation(builder.Configuration);
builder.Services.AddExceptionHandler<PrescriptionExceptionHandler>();
builder.Services.AddCentralExceptionHandler(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Authentication:JwtBearer:Authority"];
        opts.Audience = builder.Configuration["Authentication:JwtBearer:Audience"] ?? "api://dialysis-pdms";
        opts.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:JwtBearer:RequireHttpsMetadata", true);
    });
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PrescriptionRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Prescription:Read", "Prescription:Admin")))
    .AddPolicy("PrescriptionWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Prescription:Write", "Prescription:Admin")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(GetPrescriptionByMrnQuery).Assembly);
});

string connectionString = builder.Configuration.GetConnectionString("PrescriptionDb")
                          ?? "Host=localhost;Database=dialysis_prescription;Username=postgres;Password=postgres";

builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddDbContext<PrescriptionDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));
builder.Services.AddDbContext<PrescriptionReadDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<PrescriptionReadStore>();
string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    _ = builder.Services.AddTransponderRedisCache(opts => opts.ConnectionString = redisConnectionString);
    _ = builder.Services.AddReadThroughCache();
}
else _ = builder.Services.AddNullReadThroughCache();
builder.Services.AddScoped<IPrescriptionReadStore>(sp => new CachedPrescriptionReadStore(
    sp.GetRequiredService<PrescriptionReadStore>(),
    sp.GetRequiredService<IReadThroughCache>()));

builder.Services.AddScoped<IQbpD01Parser, QbpD01Parser>();
builder.Services.AddScoped<IRspK22Parser, RspK22Parser>();
builder.Services.AddScoped<IRspK22Builder, RspK22Builder>();
builder.Services.AddScoped<IRspK22Validator, RspK22Validator>();
builder.Services.AddFhirAuditRecorder();
builder.Services.AddTenantResolution();
builder.Services.Configure<PrescriptionIngestionOptions>(
    builder.Configuration.GetSection(PrescriptionIngestionOptions.SectionName));
builder.Services.Configure<TimeSyncOptions>(builder.Configuration.GetSection(TimeSyncOptions.SectionName));

IHealthChecksBuilder healthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "prescription-db");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    _ = healthChecks.AddRedis(redisConnectionString, name: "redis");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    PrescriptionDbContext db = scope.ServiceProvider.GetRequiredService<PrescriptionDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

await app.RunAsync();
