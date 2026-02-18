using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Default registry for transport factories.
/// </summary>
public sealed class TransportRegistry : ITransportRegistry
{
    private readonly List<ITransportFactory> _factories = [];

    public TransportRegistry()
    {
    }

    public TransportRegistry(IEnumerable<ITransportFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);

        foreach (ITransportFactory factory in factories) Register(factory);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ITransportFactory> Factories => _factories.AsReadOnly();

    /// <inheritdoc />
    public void Register(ITransportFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_factories.Contains(factory)) return;

        _factories.Add(factory);
    }

    /// <inheritdoc />
    public bool TryResolve(Uri address, out ITransportFactory? factory)
    {
        ArgumentNullException.ThrowIfNull(address);

        string scheme = address.Scheme;

        factory = _factories.FirstOrDefault(candidate =>
            candidate.SupportedSchemes.Any(supported =>
                string.Equals(supported, scheme, StringComparison.OrdinalIgnoreCase)));

        return factory != null;
    }
}
