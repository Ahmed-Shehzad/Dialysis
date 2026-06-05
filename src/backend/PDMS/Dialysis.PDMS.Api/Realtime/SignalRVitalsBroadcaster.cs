using Dialysis.PDMS.TreatmentSessions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Dialysis.PDMS.Api.Realtime;

public sealed class SignalRVitalsBroadcaster : IVitalsBroadcaster
{
    private readonly IHubContext<VitalsHub> _hub;
    public SignalRVitalsBroadcaster(IHubContext<VitalsHub> hub) => _hub = hub;
    public Task BroadcastAsync(VitalsReadingSnapshot reading, CancellationToken cancellationToken)
        => _hub.Clients
            .Group(VitalsHub.GroupName(reading.SessionId))
            .SendAsync("reading", reading, cancellationToken);

    public Task BroadcastCostAsync(SessionCostSnapshot cost, CancellationToken cancellationToken)
        => _hub.Clients
            .Group(VitalsHub.GroupName(cost.SessionId))
            .SendAsync("cost", cost, cancellationToken);
}
