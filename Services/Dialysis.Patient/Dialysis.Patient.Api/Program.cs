using BuildingBlocks.Authorization;
using BuildingBlocks.Interceptors;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Features.GetPatientByMrn;
using Dialysis.Patient.Infrastructure.Persistence;

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

builder.Services.AddScoped<IPatientRepository, PatientRepository>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "patient-db");

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    PatientDbContext db = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
    _ = await db.Database.EnsureCreatedAsync();
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
