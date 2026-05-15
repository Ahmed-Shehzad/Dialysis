using Dialysis.PDMS.TreatmentSessions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Dialysis.PDMS.Api.Realtime;

public sealed class SignalRVitalsBroadcaster(IHubContext<VitalsHub> hub) : IVitalsBroadcaster
{
    public Task BroadcastAsync(VitalsReadingSnapshot reading, CancellationToken cancellationToken)
        => hub.Clients
            .Group(VitalsHub.GroupName(reading.SessionId))
            .SendAsync("reading", reading, cancellationToken);
}
