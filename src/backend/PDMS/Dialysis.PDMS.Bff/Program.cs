using Dialysis.Module.Bff;
using Dialysis.Module.Bff.Events;
using Dialysis.PDMS.Bff.Notifications;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.ServiceDefaults;

namespace Dialysis.PDMS.Bff;

/// <summary>Application entry point.</summary>
public class Program
{
    /// <summary>Builds and runs the host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddModuleBff();

        // Event-driven push: live chairside alarms for the PDMS SPA (intradialytic adverse events, …).
        // Reads still go through the synchronous proxy.
        builder.AddModuleBffEvents(transponder =>
            transponder.AddConsumer<IntradialyticAdverseEventIntegrationEvent, IntradialyticAdverseEventNotificationConsumer>());

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.MapModuleBff();
        app.MapModuleBffEvents();

        await app.RunAsync().ConfigureAwait(false);
    }
}
