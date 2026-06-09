using Dialysis.EHR.Bff.Notifications;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Module.Bff;
using Dialysis.Module.Bff.Events;
using Dialysis.ServiceDefaults;

namespace Dialysis.EHR.Bff;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddModuleBff();

        // Event-driven push: consume the integration events the EHR chart cares about and fan them out to
        // the SPA over SignalR. Lab results arrive cross-context (Lab module → RabbitMQ → this BFF's
        // bff-ehr queue → consumer → patient group → toast). Reads still go through the synchronous proxy.
        builder.AddModuleBffEvents(transponder =>
        {
            transponder.AddConsumer<LabResultReceivedIntegrationEvent, LabResultNotificationConsumer>();
            // Care coordination: hospital admit/discharge → live "patient in hospital" toast.
            transponder.AddConsumer<PatientAdmittedIntegrationEvent, HospitalAdmitNotificationConsumer>();
            transponder.AddConsumer<PatientDischargedIntegrationEvent, HospitalDischargeNotificationConsumer>();
            // Patient participation: a patient's inbound secure message → "new patient message" toast on the chart.
            transponder.AddConsumer<Contracts.Integration.PatientPortalSecureMessageSentIntegrationEvent, SecureMessageSentNotificationConsumer>();
            // A patient's appointment request → "new appointment request" toast for staff.
            transponder.AddConsumer<Contracts.Integration.PatientPortalAppointmentRequestedIntegrationEvent, AppointmentRequestedNotificationConsumer>();
        });

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.MapModuleBff();
        app.MapModuleBffEvents();

        await app.RunAsync().ConfigureAwait(false);
    }
}
