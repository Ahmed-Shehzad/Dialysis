using BuildingBlocks.Authorization;
using BuildingBlocks.Tenancy;

using Dialysis.Cds.Api;

using Microsoft.AspNetCore.Authentication.JwtBearer;

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
    .AddPolicy("CdsRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Treatment:Read", "Prescription:Read")));
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PrescriptionComplianceService>();
builder.Services.AddSingleton<HypotensionRiskService>();
builder.Services.AddSingleton<VenousPressureRiskService>();
builder.Services.AddSingleton<BloodLeakRiskService>();
builder.Services.AddTenantResolution();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

WebApplication app = builder.Build();

app.UseTenantResolution();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
