using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Tenancy;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Intercessor;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

using Verifier.Exceptions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<PrescriptionDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IQbpD01Parser, QbpD01Parser>();
builder.Services.AddScoped<IRspK22Parser, RspK22Parser>();
builder.Services.AddScoped<IRspK22Builder, RspK22Builder>();
builder.Services.AddScoped<IRspK22Validator, RspK22Validator>();
builder.Services.AddAuditRecorder();
builder.Services.AddTenantResolution();

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

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
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
        if (exception is ArgumentException argEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = argEx.Message });
            return;
        }
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

await app.RunAsync();
