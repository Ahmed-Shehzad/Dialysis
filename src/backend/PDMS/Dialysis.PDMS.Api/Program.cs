using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Module.Hosting;
using Dialysis.PDMS.Composition;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string connectionStringName = "Pdms";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Pdms:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Pdms:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Pdms:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Pdms:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<PdmsPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "pdms",
    HandlerAssemblies = [typeof(PdmsTreatmentSessionsMarker).Assembly],
});

builder.Services.AddPatientDataManagementSystem(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "pdms")),
    enableOutboxRelay: enableOutbox,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        }));

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { module = "pdms", version = "v1" }));

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.PDMS.Api
{
    public partial class Program;
}
