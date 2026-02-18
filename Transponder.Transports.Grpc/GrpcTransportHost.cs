using System.Collections.Concurrent;

using Grpc.Net.Client;

using Microsoft.Extensions.Http.Resilience;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Grpc.Abstractions;

namespace Transponder.Transports.Grpc;

/// <summary>
/// gRPC transport host backed by gRPC channel and server endpoints.
/// </summary>
public sealed class GrpcTransportHost : TransportHostBase
{
    private readonly GrpcChannel _channel;
    private readonly ConcurrentDictionary<Uri, GrpcReceiveEndpoint> _endpoints = new();
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public GrpcTransportHost(IGrpcHostSettings settings)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
        var handler = new SocketsHttpHandler();

        if (settings.KeepAliveTime.HasValue)
        {
            handler.KeepAlivePingDelay = settings.KeepAliveTime.Value;
            handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(20);
        }

        HttpMessageHandler httpHandler = handler;
        ResiliencePipeline<HttpResponseMessage> httpPipeline = TransportResiliencePipeline.CreateHttpPipeline(_resilienceOptions);

        if (!ReferenceEquals(httpPipeline, ResiliencePipeline<HttpResponseMessage>.Empty))
        {
            var resilienceHandler = new ResilienceHandler(httpPipeline)
            {
                InnerHandler = handler
            };

            httpHandler = resilienceHandler;
        }

        var options = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = settings.MaxReceiveMessageSize,
            MaxSendMessageSize = settings.MaxSendMessageSize,
            HttpHandler = httpHandler
        };

        _channel = GrpcChannel.ForAddress(settings.Address, options);
    }

    public IGrpcHostSettings Settings { get; }

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        var transport = new GrpcSendTransport(_channel, address);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var transport = new GrpcPublishTransport(_channel);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ReceiveEndpointFaultSettings? faultSettings = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);
        ResiliencePipeline pipeline = TransportResiliencePipeline.Create(faultSettings?.ResilienceOptions ?? _resilienceOptions);
        var endpoint = new GrpcReceiveEndpoint(this, configuration, faultSettings, pipeline);
        return !_endpoints.TryAdd(configuration.InputAddress, endpoint)
            ? throw new InvalidOperationException(
                $"A receive endpoint is already registered for '{configuration.InputAddress}'.")
            : (IReceiveEndpoint)endpoint;
    }

    internal bool TryGetEndpoint(Uri address, out GrpcReceiveEndpoint endpoint)
        => _endpoints.TryGetValue(address, out endpoint!);

    internal IReadOnlyCollection<GrpcReceiveEndpoint> GetEndpoints()
        => new List<GrpcReceiveEndpoint>(_endpoints.Values);

    public override ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
