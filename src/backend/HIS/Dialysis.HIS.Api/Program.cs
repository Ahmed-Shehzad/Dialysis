using System.Threading.RateLimiting;
using Asp.Versioning;
using Dialysis.HIS.Api.Authorization;
using Dialysis.HIS.Api.OpenApi;
using Dialysis.HIS.Composition;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetValue("His:UseForwardedHeaders", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    });
}

// Must match HisDbContextDesignTimeFactory.ConnectionStringName ("His") for shared appsettings / env.
const string hisSqlServerConnectionName = "His";
var sqlConnection = builder.Configuration.GetConnectionString(hisSqlServerConnectionName);
var enableOutbox = builder.Configuration.GetValue("His:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["His:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["His:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["His:Transponder:RabbitMq:ExchangeName"];

builder.Services.AddHospitalInformationSystem(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(sqlConnection)
        ? null
        : options => options.UseSqlServer(
            sqlConnection,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "his_migrations")),
    enableOutboxDispatcher: enableOutbox,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddHisTransponderRabbitMqIfConfigured(rabbitUri, rabbitQueue, rabbitExchange));

var authAuthority = builder.Configuration["His:Authentication:Authority"];
if (!string.IsNullOrWhiteSpace(authAuthority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.Authority = authAuthority;
            var audience = builder.Configuration["His:Authentication:Audience"];
            if (!string.IsNullOrWhiteSpace(audience))
                o.Audience = audience;
            if (Uri.TryCreate(authAuthority, UriKind.Absolute, out var issuer)
                && string.Equals(issuer.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                && builder.Environment.IsDevelopment())
                o.RequireHttpsMetadata = false;
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("DeviceIngest", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

builder.Services.AddScoped<PatientPortalPatientScopeFilter>();
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = HisOpenApiDocuments.ExplorerGroupNameFormat;
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddHisVersionedOpenApi();

var app = builder.Build();
if (builder.Configuration.GetValue("His:UseForwardedHeaders", false))
    app.UseForwardedHeaders();

if (builder.Configuration.GetValue("His:RequireHttpsRedirection", false) && !app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

if (builder.Configuration.GetValue("His:UseHsts", false) && !app.Environment.IsDevelopment())
    app.UseHsts();

if (!string.IsNullOrWhiteSpace(authAuthority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseRateLimiter();
app.MapOpenApi();
app.MapControllers();
await app.RunAsync().ConfigureAwait(false);
