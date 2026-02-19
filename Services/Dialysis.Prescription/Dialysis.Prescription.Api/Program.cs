using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Logging;
using BuildingBlocks.Tenancy;
using BuildingBlocks.TimeSync;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;
using Dialysis.Prescription.Application.Features.ProcessQbpD01Query;
using Dialysis.Prescription.Application.Options;
using Dialysis.Prescription.Infrastructure;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Intercessor;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

using Serilog;

using Verifier.Exceptions;

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
    .AddPolicy("PrescriptionRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Prescription:Read", "Prescription:Admin")))
    .AddPolicy("PrescriptionWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Prescription:Write", "Prescription:Admin")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(typeof(GetPrescriptionByMrnQuery).Assembly);
});
// Explicit registration ensures handler is resolvable when Scrutor/assembly scan misses it (e.g. in Docker)
builder.Services.AddTransient<Intercessor.Abstractions.IRequestHandler<ProcessQbpD01QueryCommand, ProcessQbpD01QueryResponse>, ProcessQbpD01QueryCommandHandler>();

string connectionString = builder.Configuration.GetConnectionString("PrescriptionDb")
                          ?? "Host=localhost;Database=dialysis_prescription;Username=postgres;Password=postgres";

builder.Services.AddDbContext<PrescriptionDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddDbContext<PrescriptionReadDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IPrescriptionReadStore, PrescriptionReadStore>();
builder.Services.AddScoped<IQbpD01Parser, QbpD01Parser>();
builder.Services.AddScoped<IRspK22Parser, RspK22Parser>();
builder.Services.AddScoped<IRspK22Builder, RspK22Builder>();
builder.Services.AddScoped<IRspK22Validator, RspK22Validator>();
builder.Services.AddFhirAuditRecorder();
builder.Services.AddTenantResolution();
builder.Services.Configure<PrescriptionIngestionOptions>(
    builder.Configuration.GetSection(PrescriptionIngestionOptions.SectionName));
builder.Services.Configure<TimeSyncOptions>(builder.Configuration.GetSection(TimeSyncOptions.SectionName));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "prescription-db");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    PrescriptionDbContext db = scope.ServiceProvider.GetRequiredService<PrescriptionDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(HandleExceptionAsync));

async static Task HandleExceptionAsync(HttpContext context)
{
    Exception? exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (exception is ValidationException validationException)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        var errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
        await context.Response.WriteAsJsonAsync(new { errors });
        return;
    }
    if (exception is Dialysis.Prescription.Application.Exceptions.RspK22ValidationException rspEx)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { errorCode = rspEx.ErrorCode, message = rspEx.Message });
        return;
    }
    if (exception is Dialysis.Prescription.Application.Exceptions.PrescriptionConflictException conflictEx)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/json";
        var body = new Dictionary<string, object?> { ["orderId"] = conflictEx.OrderId, ["message"] = conflictEx.Message };
        if (!string.IsNullOrEmpty(conflictEx.CallbackPhone))
            body["callbackPhone"] = conflictEx.CallbackPhone;
        await context.Response.WriteAsJsonAsync(body);
        return;
    }
    if (exception is ArgumentException argEx)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = argEx.Message });
        return;
    }
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
}

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

await app.RunAsync();
