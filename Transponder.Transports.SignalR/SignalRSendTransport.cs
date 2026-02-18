using Microsoft.AspNetCore.SignalR;

using Transponder.Transports.Abstractions;
using Transponder.Transports.SignalR.Abstractions;

namespace Transponder.Transports.SignalR;

internal sealed class SignalRSendTransport : ISendTransport
{
    private readonly Uri _destination;
    private readonly IHubContext<TransponderSignalRHub> _hubContext;
    private readonly ISignalRHostSettings _settings;

    public SignalRSendTransport(
        Uri destination,
        IHubContext<TransponderSignalRHub> hubContext,
        ISignalRHostSettings settings)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SignalRPublishTargets targets = SignalRPublishTargetResolver.Resolve(
            message,
            _destination,
            allowDefaultBroadcast: false);

        if (!targets.Broadcast &&
            targets.ConnectionIds.Count == 0 &&
            targets.Groups.Count == 0 &&
            targets.Users.Count == 0 &&
            targets.ExcludedConnectionIds.Count == 0)
            throw new InvalidOperationException(
                $"SignalR send requires a target. Destination='{_destination}'.");

        var envelope = SignalRTransportEnvelope.From(message);
        string method = _settings.Topology.SendMethodName;

        if (targets.ConnectionIds.Count > 0)
        {
            await _hubContext.Clients.Clients(targets.ConnectionIds)
                .SendAsync(method, envelope, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (targets.Users.Count > 0 && targets.Groups.Count > 0)
        {
            await Task.WhenAll(
                    _hubContext.Clients.Users(targets.Users).SendAsync(method, envelope, cancellationToken),
                    _hubContext.Clients.Groups(targets.Groups).SendAsync(method, envelope, cancellationToken))
                .ConfigureAwait(false);
            return;
        }

        if (targets.Users.Count > 0)
        {
            await _hubContext.Clients.Users(targets.Users)
                .SendAsync(method, envelope, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (targets.Groups.Count > 0)
        {
            await _hubContext.Clients.Groups(targets.Groups)
                .SendAsync(method, envelope, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (targets.ExcludedConnectionIds.Count > 0)
        {
            await _hubContext.Clients.AllExcept(targets.ExcludedConnectionIds)
                .SendAsync(method, envelope, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (targets.Broadcast)
        {
            await _hubContext.Clients.All.SendAsync(method, envelope, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _hubContext.Clients.All.SendAsync(method, envelope, cancellationToken).ConfigureAwait(false);
    }
}
