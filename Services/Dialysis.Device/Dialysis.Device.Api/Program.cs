using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Tenancy;

using Dialysis.Device.Application.Abstractions;
using Dialysis.Device.Application.Features.RegisterDevice;
using Dialysis.Device.Infrastructure.Persistence;

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

builder.Services.AddDbContext<DeviceDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddAuditRecorder();
builder.Services.AddTenantResolution();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "device-db");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    DeviceDbContext db = scope.ServiceProvider.GetRequiredService<DeviceDbContext>();
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
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapControllers();

await app.RunAsync();
