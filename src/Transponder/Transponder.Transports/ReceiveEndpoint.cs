using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Default receive endpoint backed by a message handler delegate.
/// </summary>
internal sealed class ReceiveEndpoint : IReceiveEndpoint
{
    private readonly Func<IReceiveContext, Task> _handler;

    public ReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        InputAddress = configuration.InputAddress;
        _handler = configuration.Handler ?? throw new ArgumentNullException(nameof(configuration.Handler));
        Settings = configuration.Settings;
    }

    public Uri InputAddress { get; }

    internal IReadOnlyDictionary<string, object?> Settings { get; }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => new(StopAsync());

    internal Task HandleAsync(
        ITransportMessage message,
        Uri? sourceAddress,
        Uri? destinationAddress,
        CancellationToken cancellationToken)
    {
        var context = new ReceiveContext(message, sourceAddress, destinationAddress, cancellationToken);
        return _handler(context);
    }
}
