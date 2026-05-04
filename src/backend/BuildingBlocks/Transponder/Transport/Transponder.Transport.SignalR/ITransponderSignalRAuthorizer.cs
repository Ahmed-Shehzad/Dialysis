using Microsoft.AspNetCore.SignalR;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Optional hook to authorize hub <see cref="TransponderSignalRHub.Publish"/> and new connections (subscribe).</summary>
public interface ITransponderSignalRAuthorizer
{
    ValueTask AuthorizePublishAsync(
        HubCallerContext context,
        TransponderSignalREnvelopeDto envelope,
        CancellationToken cancellationToken = default);

    /// <summary>Called from <see cref="TransponderSignalRHub.OnConnectedAsync"/> before the connection is considered ready to receive.</summary>
    ValueTask AuthorizeSubscribeAsync(HubCallerContext context, CancellationToken cancellationToken = default);
}
