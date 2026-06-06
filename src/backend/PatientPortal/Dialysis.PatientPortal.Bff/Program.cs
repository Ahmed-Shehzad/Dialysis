using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff;
using Dialysis.Module.Bff.Events;
using Dialysis.PatientPortal.Bff.Notifications;
using Dialysis.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddModuleBff();

// Event-driven push: the portal consumes the patient-facing integration events and fans them out to
// the patient's SPA session over SignalR (queue bff-portal). Reads stay on the synchronous proxy.
builder.AddModuleBffEvents(transponder =>
{
    transponder.AddConsumer<PatientPortalSecureMessageReceivedIntegrationEvent, SecureMessageReceivedNotificationConsumer>();
    transponder.AddConsumer<PatientPortalAppointmentResolvedIntegrationEvent, AppointmentResolvedNotificationConsumer>();
});

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapModuleBff();
app.MapModuleBffEvents();

await app.RunAsync().ConfigureAwait(false);
