using Dialysis.EHR.Bff.Notifications;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Module.Bff;
using Dialysis.Module.Bff.Events;
using Dialysis.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddModuleBff();

// Event-driven push: consume the integration events the EHR chart cares about and fan them out to
// the SPA over SignalR. Lab results arrive cross-context (Lab module → RabbitMQ → this BFF's
// bff-ehr queue → consumer → patient group → toast). Reads still go through the synchronous proxy.
builder.AddModuleBffEvents(transponder =>
    transponder.AddConsumer<LabResultReceivedIntegrationEvent, LabResultNotificationConsumer>());

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapModuleBff();
app.MapModuleBffEvents();

await app.RunAsync().ConfigureAwait(false);
