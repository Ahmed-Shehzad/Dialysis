using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Caching;
using BuildingBlocks.ExceptionHandling;
using BuildingBlocks.Logging;
using BuildingBlocks.Options;
using BuildingBlocks.Tenancy;
using BuildingBlocks.Interceptors;

using Dialysis.Patient.Application.Abstractions;

using Dialysis.Patient.Application.Features.GetPatientByMrn;
using Dialysis.Patient.Infrastructure;
using Dialysis.Patient.Infrastructure.Hl7;
using Dialysis.Patient.Infrastructure.Persistence;

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
    .AddPolicy("PatientRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Patient:Read", "Patient:Admin")))
    .AddPolicy("PatientWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Patient:Write", "Patient:Admin")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(GetPatientByMrnQuery).Assembly);
});

string connectionString = builder.Configuration.GetConnectionString("PatientDb")
                          ?? "Host=localhost;Database=dialysis_patient;Username=postgres;Password=postgres";

builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddDbContext<PatientDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));
builder.Services.AddDbContext<PatientReadDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<PatientReadStore>();
string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    _ = builder.Services.AddTransponderRedisCache(opts => opts.ConnectionString = redisConnectionString);
    _ = builder.Services.AddReadThroughCache();
}
else _ = builder.Services.AddNullReadThroughCache();
builder.Services.AddScoped<IPatientReadStore>(sp => new CachedPatientReadStore(
    sp.GetRequiredService<PatientReadStore>(),
    sp.GetRequiredService<IReadThroughCache>()));
builder.Services.AddScoped<IQbpQ22Parser, QbpQ22Parser>();
builder.Services.AddScoped<IPatientRspK22Builder, PatientRspK22Builder>();
builder.Services.AddScoped<IRspK22PatientParser, RspK22PatientParser>();
builder.Services.AddScoped<IRspK22PatientValidator, RspK22PatientValidator>();
builder.Services.AddFhirAuditRecorder();
builder.Services.AddTenantResolution();

IHealthChecksBuilder patientHealthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "patient-db");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    _ = patientHealthChecks.AddRedis(redisConnectionString, name: "redis");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    PatientDbContext db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCentralExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

await app.RunAsync();

namespace Dialysis.Patient.Api { public partial class Program { } }
