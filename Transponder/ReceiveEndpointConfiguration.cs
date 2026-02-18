using Transponder.Transports.Abstractions;

namespace Transponder;

internal sealed class ReceiveEndpointConfiguration : IReceiveEndpointConfiguration
{
    public ReceiveEndpointConfiguration(
        Uri inputAddress,
        Func<IReceiveContext, Task> handler,
        IReadOnlyDictionary<string, object?>? settings = null)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        Settings = settings ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public Uri InputAddress { get; }

    public Func<IReceiveContext, Task> Handler { get; }

    public IReadOnlyDictionary<string, object?> Settings { get; }
}
