using BuildingBlocks.Authorization;
using BuildingBlocks.Logging;
using BuildingBlocks.Tenancy;

using Dialysis.Reports.Api;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Refit;

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
    .AddPolicy("ReportsRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Treatment:Read", "Alarm:Read", "Prescription:Read")));
builder.Services.AddControllers();

string reportsBaseUrl = builder.Configuration["Reports:BaseUrl"] ?? "http://localhost:5000";
_ = builder.Services.AddRefitClient<IReportsGatewayApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(reportsBaseUrl.TrimEnd('/') + "/"))
    .AddStandardResilienceHandler();
builder.Services.AddScoped<ReportsAggregationService>();

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
