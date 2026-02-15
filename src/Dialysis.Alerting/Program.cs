using System.Reflection;
using BuildingBlocks.Abstractions;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.Alerting;
using Dialysis.Alerting.Data;
using Dialysis.Alerting.Middleware;
using Dialysis.Alerting.Services;
using Dialysis.Contracts.Events;
using Dialysis.Messaging;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("ServiceBus");
var alertingAddress = new Uri("sb://dialysis/alerting");
builder.Services.AddDialysisTransponder(alertingAddress, serviceBusConnection);
builder.Services.AddIntegrationEventConsumer<HypotensionRiskRaised>(
    new Uri("sb://dialysis/hypotension-risk-raised/subscriptions/alerting-subscription"));
builder.Services.AddScoped<IIntegrationEventHandler<HypotensionRiskRaised>, HypotensionRiskIntegrationEventHandler>();

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);
builder.Services.AddScoped<ITenantAlertDbContextFactory, TenantAlertDbContextFactory>();

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "Dialysis:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-alerting");
builder.Services.AddDialysisHealthChecks(builder.Configuration)
    .AddServiceBusHealthCheck(builder.Configuration);

builder.Services.AddScoped<IAlertCacheService, AlertCacheService>();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();
builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.ValidateProductionConfig();
app.UseTenantResolution(requireTenant: false);
app.UseJwtAuthentication();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var resolver = scope.ServiceProvider.GetRequiredService<ITenantConnectionResolver>();
    var conn = resolver.GetConnectionString("default");
    var options = new DbContextOptionsBuilder<AlertDbContext>().UseNpgsql(conn).Options;
    await using var db = new AlertDbContext(options);
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler(_ => { });
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
