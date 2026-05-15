using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dialysis.PDMS.Api.Realtime;

/// <summary>
/// SignalR hub for live intradialytic vitals. Clients call <see cref="JoinSession"/> with a
/// session id to subscribe to that session's reading stream; the server pushes
/// <c>reading</c> events as readings are recorded.
/// </summary>
[Authorize]
public sealed class VitalsHub : Hub
{
    public const string Path = "/hubs/vitals";

    public Task JoinSessionAsync(Guid sessionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));

    public Task LeaveSessionAsync(Guid sessionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sessionId));

    public static string GroupName(Guid sessionId) => $"session:{sessionId:D}";
}
