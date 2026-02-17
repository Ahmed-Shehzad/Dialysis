using System.Reflection;

using Dialysis.Gateway.Infrastructure;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.Gateway.Middleware;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;

using Intercessor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? "Host=localhost;Database=dialysis_gateway;Username=postgres;Password=postgres";

builder.Services.AddDbContext<DialysisDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly());
    cfg.RegisterFromAssembly(typeof(Dialysis.Alerting.ObservationCreatedAlertHandler).Assembly);
    cfg.RegisterFromAssembly(typeof(Dialysis.DeviceIngestion.Features.Vitals.Ingest.IngestVitalsHandler).Assembly);
});

builder.Services.AddExceptionHandler<Dialysis.Gateway.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<Dialysis.Gateway.PatientConflictExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

app.UseTenantResolution();
app.UseExceptionHandler();
app.MapControllers();
app.MapHealthChecks("/health/ready");

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DialysisDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not run migrations. Ensure PostgreSQL is available.");
    }
}

app.Run();
