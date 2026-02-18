using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Webhooks.Abstractions;

namespace Transponder.Transports.Webhooks;

/// <summary>
/// Webhook transport host.
/// </summary>
public sealed class WebhookTransportHost : TransportHostBase
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;
    private readonly ResiliencePipeline _resiliencePipeline;

    public WebhookTransportHost(IWebhookHostSettings settings)
        : this(settings, httpClient: null)
    {
    }

    internal WebhookTransportHost(IWebhookHostSettings settings, HttpClient? httpClient)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        TransportResilienceOptions? resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(resilienceOptions);
        _httpClient = httpClient ?? new HttpClient();
        _disposeClient = httpClient is null;

        if (settings.RequestTimeout.HasValue && settings.RequestTimeout.Value > TimeSpan.Zero)
            _httpClient.Timeout = settings.RequestTimeout.Value;
    }

    public IWebhookHostSettings Settings { get; }

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ISendTransport transport = new WebhookSendTransport(address);
        return Task.FromResult(transport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var transport = new WebhookPublishTransport(_httpClient, Settings);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
        => throw new NotSupportedException("Webhook transport is publish-only and does not support receive endpoints.");

    public async override ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_disposeClient) _httpClient.Dispose();
    }
}
