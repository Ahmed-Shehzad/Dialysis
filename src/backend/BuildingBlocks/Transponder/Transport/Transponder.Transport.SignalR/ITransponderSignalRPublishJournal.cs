using Microsoft.AspNetCore.SignalR;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Optional durability hook: runs after publish authorization and before fan-out to connected clients.</summary>
public interface ITransponderSignalRPublishJournal
{
    ValueTask AppendAsync(
        TransponderSignalREnvelopeDto envelope,
        HubCallerContext context,
        CancellationToken cancellationToken = default);
}
