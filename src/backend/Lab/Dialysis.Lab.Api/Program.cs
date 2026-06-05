using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Lab.Composition;
using Dialysis.Lab.Contracts.Security;
using Dialysis.Lab.Orders;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

const string connectionStringName = "Lab";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
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
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
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
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();
app.MapGet("/", () => Results.Ok(new { module = "lab", version = "v1" }));
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.Lab.Api
{
    public partial class Program;
}
