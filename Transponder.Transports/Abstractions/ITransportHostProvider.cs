namespace Transponder.Transports.Abstractions;

/// <summary>
/// Resolves transport hosts based on destination addresses.
/// </summary>
public interface ITransportHostProvider
{
    /// <summary>
    /// Gets a transport host for the specified address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    ITransportHost GetHost(Uri address);

    /// <summary>
    /// Attempts to get a transport host for the specified address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    /// <param name="host">The resolved transport host, if found.</param>
    bool TryGetHost(Uri address, out ITransportHost? host);
}
