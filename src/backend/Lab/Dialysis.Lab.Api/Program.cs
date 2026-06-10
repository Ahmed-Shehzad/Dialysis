using System.Text.Json.Serialization;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Lab.Composition;
using Dialysis.Lab.Contracts.Security;
using Dialysis.Lab.Orders;
using Dialysis.Lab.Persistence;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Lab.Api;

/// <summary>Application entry point.</summary>
public class Program
{
    /// <summary>Builds and runs the host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        const string connectionStringName = "Lab";
        var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"ConnectionStrings:{connectionStringName} must be set — this module persists to PostgreSQL.");
        var enableOutbox = builder.Configuration.GetValue("Lab:Transponder:EnableOutboxRelay", false);
        var rabbitUri = builder.Configuration["Lab:Transponder:RabbitMq:ConnectionUri"];
        var rabbitQueue = builder.Configuration["Lab:Transponder:RabbitMq:QueueName"];
        var rabbitExchange = builder.Configuration["Lab:Transponder:RabbitMq:ExchangeName"];

        builder.AddModuleHost<LabPermissionCatalog>(new ModuleHostingOptions
        {
            ModuleSlug = "lab",
            HandlerAssemblies = [typeof(LabOrdersMarker).Assembly],
        });

        builder.Services.AddLaboratory(
            builder.Configuration,
            configurePersistence: options => options.UseNpgsql(
                    connectionString,
                    pg => pg.MigrationsHistoryTable("__ef_migrations", "lab")),
            enableOutboxRelay: enableOutbox,
            configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
                ? null
                : s => s.AddTransponderRabbitMq(o =>
                {
                    o.ConnectionUri = rabbitUri;
                    if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
                    if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
                }));

        builder.Services
            .AddControllers()
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        var app = builder.Build();

        // Apply EF migrations on startup so the Lab module is self-hosting against a fresh
        // Aspire-managed Postgres container. Gated on configuration (Lab:AutoMigrate, default
        // true in Development) — in production the DBA owns the migration step out-of-band.
        if (!string.IsNullOrWhiteSpace(connectionString)
            && builder.Configuration.GetValue("Lab:AutoMigrate", app.Environment.IsDevelopment()))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
            await db.Database.MigrateAsync().ConfigureAwait(false);
        }

        app.UseModuleHost();
        app.MapOpenApi();
        app.MapGet("/", () => Results.Ok(new { module = "lab", version = "v1" }));
        app.MapControllers();

        await app.RunAsync().ConfigureAwait(false);
    }
}
