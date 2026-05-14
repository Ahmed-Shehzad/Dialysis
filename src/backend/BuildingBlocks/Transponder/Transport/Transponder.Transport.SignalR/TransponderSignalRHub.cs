using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>
/// Ingress hub: clients call <see cref="PublishAsync"/>; all connections receive copies on <see cref="ReceiveMethod"/>.
/// </summary>
public sealed class TransponderSignalRHub(ILogger<TransponderSignalRHub> logger, IServiceProvider services) : Hub
{
    /// <summary>Default path for <c>MapHub&lt;TransponderSignalRHub&gt;</c>.</summary>
    public const string MapPath = "/hubs/transponder";

    /// <summary>Hub method name invoked by publishers. Must match the C# method name on this hub.</summary>
    public const string PublishMethod = "PublishAsync";

    /// <summary>Client callback name for inbound envelopes.</summary>
    public const string ReceiveMethod = "Receive";

    public async Task PublishAsync(TransponderSignalREnvelopeDto envelope)
    {
        if (string.IsNullOrEmpty(envelope.RoutingKey))
            throw new HubException("routing_key is required.");

        if (services.GetService<ITransponderSignalRAuthorizer>() is { } authorizer)
            await authorizer.AuthorizePublishAsync(Context, envelope, Context.ConnectionAborted).ConfigureAwait(false);

        if (services.GetService<ITransponderSignalRPublishJournal>() is { } journal)
            await journal.AppendAsync(envelope, Context, Context.ConnectionAborted).ConfigureAwait(false);

        await Clients.All.SendAsync(ReceiveMethod, envelope, cancellationToken: Context.ConnectionAborted)
            .ConfigureAwait(false);

        logger.LogDebug(
            "Transponder SignalR fan-out {RoutingKey} from connection {ConnectionId}",
            envelope.RoutingKey,
            Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        if (services.GetService<ITransponderSignalRAuthorizer>() is { } authorizer)
            await authorizer.AuthorizeSubscribeAsync(Context, Context.ConnectionAborted).ConfigureAwait(false);

        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}
