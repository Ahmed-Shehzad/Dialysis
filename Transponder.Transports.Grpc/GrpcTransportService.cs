using System.Diagnostics;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc;

/// <summary>
/// gRPC service that dispatches incoming transport messages to receive endpoints.
/// </summary>
public sealed class GrpcTransportService : Transport.TransportBase
{
    private const string MissingEndpointMessage = "No matching receive endpoint.";
    private readonly ITransportHostProvider _hostProvider;
    private readonly IReadOnlyCollection<GrpcTransportHost> _hosts;
    private readonly ILogger<GrpcTransportService> _logger;

    public GrpcTransportService(
        ITransportHostProvider hostProvider,
        IEnumerable<ITransportHost> hosts,
        ILogger<GrpcTransportService> logger)
    {
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        ArgumentNullException.ThrowIfNull(hosts);
        _hosts = [.. hosts.OfType<GrpcTransportHost>()];
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async override Task<SendResponse> Send(SendRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Destination)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Destination is required."));

        var destination = new Uri(request.Destination);

        if (!TryResolveEndpoint(destination, out GrpcTransportHost grpcHost, out GrpcReceiveEndpoint endpoint))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                string knownEndpoints = string.Join(
                    " | ",
                    _hosts.Select(host =>
                    {
                        string endpoints = string.Join(", ", host.GetEndpoints().Select(entry => entry.InputAddress.ToString()));
                        return $"{host.Address} => [{endpoints}]";
                    }));
                _logger.LogWarning(
                    "GrpcTransportService no endpoint for destination {Destination}. Known endpoints: {Endpoints}",
                    destination,
                    knownEndpoints);
            }
#if DEBUG
            Trace.TraceWarning(
                $"[Transponder] GrpcTransportService no endpoint for destination {destination}.");
#endif
            throw new RpcException(new Status(StatusCode.NotFound, MissingEndpointMessage));
        }

#if DEBUG
        Trace.TraceInformation(
            $"[Transponder] GrpcTransportService dispatching to {destination}.");
#endif
        ITransportMessage message = GrpcTransportMessageMapper.FromProto(request.Message);
        await endpoint.HandleAsync(message, grpcHost.Address, destination, context.CancellationToken).ConfigureAwait(false);
        return new SendResponse();
    }

    public async override Task<PublishResponse> Publish(PublishRequest request, ServerCallContext context)
    {
        ITransportMessage message = GrpcTransportMessageMapper.FromProto(request.Message);
        IReadOnlyCollection<(GrpcTransportHost Host, GrpcReceiveEndpoint Endpoint)> endpoints = [.. _hosts.SelectMany(host => host.GetEndpoints().Select(endpoint => (host, endpoint)))];

#if DEBUG
        Trace.TraceInformation(
            $"[Transponder] GrpcTransportService publishing to {endpoints.Count} endpoints.");
#endif
        foreach ((GrpcTransportHost host, GrpcReceiveEndpoint endpoint) in endpoints)
            await endpoint.HandleAsync(message, host.Address, endpoint.InputAddress, context.CancellationToken)
                .ConfigureAwait(false);

        return new PublishResponse();
    }

    private bool TryResolveEndpoint(
        Uri destination,
        out GrpcTransportHost host,
        out GrpcReceiveEndpoint endpoint)
    {
        foreach (GrpcTransportHost candidate in _hosts)
        {
            if (candidate.TryGetEndpoint(destination, out endpoint!))
            {
                host = candidate;
                return true;
            }

            GrpcReceiveEndpoint? fallback = candidate
                .GetEndpoints()
                .FirstOrDefault(entry => UriMatches(entry.InputAddress, destination));
            if (fallback is not null)
            {
                host = candidate;
                endpoint = fallback;
                return true;
            }
        }

        try
        {
            ITransportHost resolved = _hostProvider.GetHost(destination);
            if (resolved is GrpcTransportHost grpcHost &&
                grpcHost.TryGetEndpoint(destination, out endpoint!))
            {
                host = grpcHost;
                return true;
            }
        }
        catch (Exception)
        {
            // Ignore and return false below.
        }

        host = null!;
        endpoint = null!;
        return false;
    }

    private static bool UriMatches(Uri left, Uri right)
        => Uri.Compare(left, right, UriComponents.HttpRequestUrl, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
}
