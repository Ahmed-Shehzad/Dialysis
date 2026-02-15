using System.Reflection;
using Dialysis.AuditConsent;
using Dialysis.AuditConsent.Data;
using Dialysis.AuditConsent.Middleware;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-audit-consent");
builder.Services.AddDialysisHealthChecks(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddTenancy(builder.Configuration);
builder.Services.AddScoped<ITenantAuditDbContextFactory, TenantAuditDbContextFactory>();
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
app.UseTenantResolution();
app.UseJwtAuthentication();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var resolver = scope.ServiceProvider.GetRequiredService<ITenantConnectionResolver>();
    var conn = resolver.GetConnectionString("default");
    var options = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(conn).Options;
    await using var db = new AuditDbContext(options);
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler(_ => { });
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
