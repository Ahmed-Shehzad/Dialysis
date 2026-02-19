using BuildingBlocks.Audit;
using BuildingBlocks.Authorization;
using BuildingBlocks.Logging;
using BuildingBlocks.Tenancy;
using BuildingBlocks.Interceptors;
using BuildingBlocks.TimeSync;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.Services;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;
using Dialysis.Treatment.Application.Features.GetTreatmentSessions;
using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Infrastructure;
using Dialysis.Treatment.Infrastructure.DeviceRegistration;
using Dialysis.Treatment.Infrastructure.FhirSubscription;
using Dialysis.Treatment.Infrastructure.Hl7;
using Dialysis.Treatment.Infrastructure.Persistence;

using Intercessor;
using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

using Transponder;
using Transponder.Transports.SignalR;

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
        opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                string? token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("TreatmentRead", p => p.Requirements.Add(new ScopeOrBypassRequirement("Treatment:Read", "Treatment:Admin")))
    .AddPolicy("TreatmentWrite", p => p.Requirements.Add(new ScopeOrBypassRequirement("Treatment:Write", "Treatment:Admin")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddTransponder(new Uri("transponder://treatment"), opts =>
    opts.TransportBuilder.UseSignalR(new Uri("signalr://treatment")));

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
    cfg.RegisterFromAssembly(typeof(GetTreatmentSessionQuery).Assembly);
});
builder.Services.AddScoped<IRequestHandler<GetTreatmentSessionsQuery, GetTreatmentSessionsResponse>, GetTreatmentSessionsQueryHandler>();

string connectionString = builder.Configuration.GetConnectionString("TreatmentDb")
                          ?? "Host=localhost;Database=dialysis_treatment;Username=postgres;Password=postgres";

builder.Services.AddScoped<DomainEventDispatcherInterceptor>();
builder.Services.AddDbContext<TreatmentDbContext>((sp, o) =>
    o.UseNpgsql(connectionString)
     .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));
builder.Services.AddDbContext<TreatmentReadDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddScoped<ITreatmentSessionRepository, TreatmentSessionRepository>();
builder.Services.AddScoped<ITreatmentReadStore, TreatmentReadStore>();
builder.Services.AddScoped<IOruMessageParser, OruR01Parser>();
builder.Services.AddDeviceRegistrationClient(builder.Configuration);
builder.Services.AddSingleton<IHl7BatchParser, Hl7BatchParser>();
builder.Services.AddSingleton<IAckR01Builder, AckR01Builder>();
builder.Services.AddSingleton<VitalSignsMonitoringService>();
builder.Services.AddAuditRecorder();
builder.Services.AddTenantResolution();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFhirSubscriptionNotifyClient, FhirSubscriptionNotifyClient>();

builder.Services.Configure<TimeSyncOptions>(builder.Configuration.GetSection(TimeSyncOptions.SectionName));
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "treatment-db");

WebApplication app = builder.Build();

app.UseTenantResolution();
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    TreatmentDbContext db = scope.ServiceProvider.GetRequiredService<TreatmentDbContext>();
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
app.MapHub<TransponderSignalRHub>("/transponder/transport").RequireAuthorization("TreatmentRead");
app.MapControllers();

await app.RunAsync();
