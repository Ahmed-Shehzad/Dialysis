using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Dialysis.Simulation.Composition;
using Dialysis.Simulation.Contracts.Security;
using Dialysis.Simulation.Engine;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

const string connectionStringName = "Simulation";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Simulation:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Simulation:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Simulation:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Simulation:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<SimulationPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "simulation",
    HandlerAssemblies = [typeof(SimulationEngineMarker).Assembly],
});

builder.Services.AddSimulation(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "simulation")),
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
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();
app.MapGet("/", () => Results.Ok(new { module = "simulation", version = "v1" }));
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.Simulation.Api
{
    /// <summary>Entry-point marker for <c>WebApplicationFactory</c> integration tests.</summary>
    public partial class Program;
}
