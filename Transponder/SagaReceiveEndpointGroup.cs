using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Transponder.Transports.Abstractions;

namespace Transponder;

internal sealed class SagaReceiveEndpointGroup : IReceiveEndpoint
{
    private readonly SagaEndpointRegistry _registry;
    private readonly ITransportHostProvider _hostProvider;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SagaReceiveEndpointHandler> _logger;
    private readonly ReceiveEndpointFaultSettings? _faultSettings;
    private readonly List<IReceiveEndpoint> _endpoints = [];
    private bool _started;

    public SagaReceiveEndpointGroup(
        SagaEndpointRegistry registry,
        ITransportHostProvider hostProvider,
        IMessageSerializer serializer,
        IServiceScopeFactory scopeFactory,
        ILogger<SagaReceiveEndpointHandler> logger,
        ReceiveEndpointFaultSettings? faultSettings = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _faultSettings = faultSettings;
        InputAddress = new Uri("transponder://saga");
    }

    public Uri InputAddress { get; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started) return;

        _started = true;
        foreach (Uri inputAddress in _registry.GetInputAddresses())
        {
            var handler = new SagaReceiveEndpointHandler(
                inputAddress,
                _registry,
                _serializer,
                _scopeFactory,
                _logger);

            IReadOnlyDictionary<string, object?>? settings = null;
            if (_faultSettings is not null)
                settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [ReceiveEndpointSettingsKeys.FaultSettings] = _faultSettings
                };

            var configuration = new ReceiveEndpointConfiguration(
                inputAddress,
                handler.HandleAsync,
                settings);

            ITransportHost host = _hostProvider.GetHost(inputAddress);
            IReceiveEndpoint endpoint = host.ConnectReceiveEndpoint(configuration);
            _endpoints.Add(endpoint);
        }

        foreach (IReceiveEndpoint endpoint in _endpoints) await endpoint.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started) return;

        foreach (IReceiveEndpoint endpoint in _endpoints)
        {
            await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);
            await endpoint.DisposeAsync().ConfigureAwait(false);
        }

        _endpoints.Clear();
        _started = false;
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}
