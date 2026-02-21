using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Caching;
using BuildingBlocks.ExceptionHandling;
using BuildingBlocks.Logging;
using BuildingBlocks.Tenancy;
using BuildingBlocks.Interceptors;

using Dialysis.Device.Application.Abstractions;
using Dialysis.Device.Application.Features.RegisterDevice;
using Dialysis.Device.Infrastructure;
using Dialysis.Device.Infrastructure.Persistence;

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Authentication:JwtBearer:Authority"];
        opts.Audience = builder.Configuration["Authentication:JwtBearer:Audience"] ?? "api://dialysis-pdms";
        opts.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:JwtBearer:RequireHttpsMetadata", true);
    });
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("DeviceRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Device:Read", "Device:Admin")))
    .AddPolicy("DeviceWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Device:Write", "Device:Admin")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(RegisterDeviceCommand).Assembly);
});

string connectionString = builder.Configuration.GetConnectionString("DeviceDb")
                          ?? "Host=localhost;Database=dialysis_device;Username=postgres;Password=postgres";

builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddDbContext<DeviceDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));
builder.Services.AddDbContext<DeviceReadDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<DeviceReadStore>();
string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    _ = builder.Services.AddTransponderRedisCache(opts => opts.ConnectionString = redisConnectionString);
    _ = builder.Services.AddReadThroughCache();
}
else _ = builder.Services.AddNullReadThroughCache();
builder.Services.AddScoped<IDeviceReadStore>(sp => new CachedDeviceReadStore(
    sp.GetRequiredService<DeviceReadStore>(),
    sp.GetRequiredService<IReadThroughCache>()));
builder.Services.AddFhirAuditRecorder();
builder.Services.AddTenantResolution();

IHealthChecksBuilder deviceHealthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "device-db");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
    _ = deviceHealthChecks.AddRedis(redisConnectionString, name: "redis");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    DeviceDbContext db = scope.ServiceProvider.GetRequiredService<DeviceDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCentralExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

await app.RunAsync();
