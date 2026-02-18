using System.Collections.Concurrent;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Base transport host that uses in-memory dispatching.
/// </summary>
public abstract class TransportHostBase : ITransportHost
{
    private readonly ConcurrentDictionary<Uri, ReceiveEndpoint> _endpoints = new();

    protected TransportHostBase(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        Address = address;
    }

    /// <inheritdoc />
    public Uri Address { get; }

    /// <inheritdoc />
    public virtual Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public async virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new List<ReceiveEndpoint>(_endpoints.Values);

        foreach (ReceiveEndpoint endpoint in endpoints) await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var endpoint = new ReceiveEndpoint(configuration);

        return !_endpoints.TryAdd(configuration.InputAddress, endpoint)
            ? throw new InvalidOperationException(
                $"A receive endpoint is already registered for '{configuration.InputAddress}'.")
            : (IReceiveEndpoint)endpoint;
    }

    /// <inheritdoc />
    public virtual Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ISendTransport transport = new InMemorySendTransport(this, address);
        return Task.FromResult(transport);
    }

    /// <inheritdoc />
    public virtual Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        IPublishTransport transport = new InMemoryPublishTransport(this);
        return Task.FromResult(transport);
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync() => new(StopAsync());

    internal bool TryGetEndpoint(Uri address, out ReceiveEndpoint endpoint)
        => _endpoints.TryGetValue(address, out endpoint!);

    internal IReadOnlyCollection<ReceiveEndpoint> GetEndpoints()
        => new List<ReceiveEndpoint>(_endpoints.Values);
}
