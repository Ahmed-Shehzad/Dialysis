using Microsoft.AspNetCore.SignalR;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.SignalR.Abstractions;

namespace Transponder.Transports.SignalR;

/// <summary>
/// SignalR transport host.
/// </summary>
public sealed class SignalRTransportHost : TransportHostBase
{
    private readonly IHubContext<TransponderSignalRHub> _hubContext;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public SignalRTransportHost(ISignalRHostSettings settings)
        : this(settings, SignalRNullHubContext.Instance)
    {
    }

    public SignalRTransportHost(
        ISignalRHostSettings settings,
        IHubContext<TransponderSignalRHub> hubContext)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
    }

    public ISignalRHostSettings Settings { get; }

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        var transport = new SignalRSendTransport(address, _hubContext, Settings);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var transport = new SignalRPublishTransport(_hubContext, Settings);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
        => throw new NotSupportedException("SignalR transport is publish-only and does not support receive endpoints.");
}
