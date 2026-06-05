using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dialysis.Module.Bff.Events;

/// <summary>
/// Per-context SignalR hub the SPA connects to at <c>{BasePath}/events</c>. Requires the BFF
/// cookie session (it shares the BFF auth pipeline), auto-joins the caller's <c>user:{sub}</c>
/// group on connect, and lets the SPA watch the selected patient via
/// <see cref="WatchPatientAsync"/>. Pushes are delivered to these groups by <see cref="IBffNotifier"/>.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    /// <summary>Client-side method name every notification is delivered on.</summary>
    public const string EventName = "notification";

    /// <summary>Group name for everything concerning one patient.</summary>
    public static string PatientGroup(string patientId) => "patient:" + patientId;

    /// <summary>Group name for everything addressed to one authenticated user (OIDC <c>sub</c>).</summary>
    public static string UserGroup(string subject) => "user:" + subject;

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var subject = Context.User?.FindFirst("sub")?.Value ?? Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(subject)).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Joins the connection to the group for <paramref name="patientId"/> (the selected patient).</summary>
    public Task WatchPatientAsync(string patientId) =>
        string.IsNullOrWhiteSpace(patientId)
            ? Task.CompletedTask
            : Groups.AddToGroupAsync(Context.ConnectionId, PatientGroup(patientId));

    /// <summary>Leaves the group for <paramref name="patientId"/> (e.g. when the SPA switches patient).</summary>
    public Task UnwatchPatientAsync(string patientId) =>
        string.IsNullOrWhiteSpace(patientId)
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, PatientGroup(patientId));
}
