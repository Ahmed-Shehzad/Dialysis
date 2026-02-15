namespace Transponder.Transports.Abstractions;

/// <summary>
/// Maintains a registry of available transports.
/// </summary>
public interface ITransportRegistry
{
    /// <summary>
    /// Gets the registered transport factories.
    /// </summary>
    IReadOnlyCollection<ITransportFactory> Factories { get; }

    /// <summary>
    /// Registers a transport factory.
    /// </summary>
    /// <param name="factory">The transport factory to register.</param>
    void Register(ITransportFactory factory);

    /// <summary>
    /// Attempts to resolve a transport factory for the specified address.
    /// </summary>
    /// <param name="address">The address to resolve.</param>
    /// <param name="factory">The resolved transport factory, if found.</param>
    bool TryResolve(Uri address, out ITransportFactory? factory);
}
