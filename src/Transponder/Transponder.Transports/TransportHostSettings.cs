using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Base settings for creating transport hosts.
/// </summary>
public abstract class TransportHostSettings : ITransportHostResilienceSettings
{
    protected TransportHostSettings(
        Uri address,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
    {
        ArgumentNullException.ThrowIfNull(address);
        Address = address;
        Settings = settings ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        ResilienceOptions = resilienceOptions;
    }

    /// <inheritdoc />
    public Uri Address { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Settings { get; }

    /// <inheritdoc />
    public TransportResilienceOptions? ResilienceOptions { get; }
}
