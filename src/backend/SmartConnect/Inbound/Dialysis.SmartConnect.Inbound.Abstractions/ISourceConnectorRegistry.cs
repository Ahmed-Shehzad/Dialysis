namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Lookup and registration for <see cref="ISourceConnector"/> implementations keyed by <see cref="ISourceConnector.Kind"/>.
/// </summary>
public interface ISourceConnectorRegistry
{
    /// <summary>Registers a source connector, replacing any existing entry for its kind.</summary>
    void Register(ISourceConnector connector);

    /// <summary>Returns the connector for <paramref name="kind"/> (case-insensitive) or <c>null</c>.</summary>
    ISourceConnector? TryResolve(string kind);

    /// <summary>Enumerates all registered connectors.</summary>
    IReadOnlyCollection<ISourceConnector> All { get; }
}
