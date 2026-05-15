using System.Collections.Concurrent;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// In-process <see cref="ISourceConnectorRegistry"/> backed by a concurrent dictionary keyed
/// case-insensitively on <see cref="ISourceConnector.Kind"/>.
/// </summary>
public sealed class SourceConnectorRegistry : ISourceConnectorRegistry
{
    private readonly ConcurrentDictionary<string, ISourceConnector> _connectors =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(ISourceConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        if (string.IsNullOrWhiteSpace(connector.Kind))
        {
            throw new ArgumentException("Source connector Kind must not be empty.", nameof(connector));
        }

        _connectors[connector.Kind] = connector;
    }

    public ISourceConnector? TryResolve(string kind) =>
        string.IsNullOrWhiteSpace(kind)
            ? null
            : _connectors.TryGetValue(kind, out var c) ? c : null;

    public IReadOnlyCollection<ISourceConnector> All => [.. _connectors.Values];
}
