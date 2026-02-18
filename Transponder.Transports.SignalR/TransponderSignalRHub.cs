using Microsoft.AspNetCore.SignalR;

namespace Transponder.Transports.SignalR;

/// <summary>
/// SignalR hub used by the Transponder realtime transport.
/// Clients call JoinGroup/LeaveGroup to subscribe to group-targeted messages.
/// </summary>
public sealed class TransponderSignalRHub : Hub
{
    /// <summary>
    /// Add the current connection to a group for targeted message delivery.
    /// </summary>
    public Task JoinGroup(string groupName) => Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    /// <summary>
    /// Remove the current connection from a group.
    /// </summary>
    public Task LeaveGroup(string groupName) => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}
