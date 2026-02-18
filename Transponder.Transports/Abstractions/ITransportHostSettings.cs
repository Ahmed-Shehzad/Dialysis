namespace Transponder.Transports.Abstractions;

/// <summary>
/// Provides base settings for creating a transport host.
/// </summary>
public interface ITransportHostSettings
{
    /// <summary>
    /// Gets the transport host address.
    /// </summary>
    Uri Address { get; }

    /// <summary>
    /// Gets transport-specific settings.
    /// </summary>
    IReadOnlyDictionary<string, object?> Settings { get; }
}
