using Dialysis.HIS.Bff.Notifications;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.Module.Bff;
using Dialysis.Module.Bff.Events;
using Dialysis.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddModuleBff();

// Event-driven push: live today-board signals for the HIS SPA (patient admissions, …). Reads
// still go through the synchronous proxy.
builder.AddModuleBffEvents(transponder =>
    transponder.AddConsumer<PatientAdmittedIntegrationEvent, PatientAdmittedNotificationConsumer>());

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapModuleBff();
app.MapModuleBffEvents();

await app.RunAsync().ConfigureAwait(false);
