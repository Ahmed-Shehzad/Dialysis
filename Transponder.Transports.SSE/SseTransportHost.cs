using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// SSE transport host.
/// </summary>
public sealed class SseTransportHost : TransportHostBase
{
    private readonly SseClientRegistry _registry;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public SseTransportHost(ISseHostSettings settings)
        : this(settings, new SseClientRegistry())
    {
    }

    public SseTransportHost(ISseHostSettings settings, SseClientRegistry registry)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
    }

    public ISseHostSettings Settings { get; }

    public SseClientRegistry ClientRegistry => _registry;

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        var transport = new SseSendTransport(address, _registry);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var transport = new SsePublishTransport(_registry);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
        => throw new NotSupportedException("SSE transport is publish-only and does not support receive endpoints.");
}
