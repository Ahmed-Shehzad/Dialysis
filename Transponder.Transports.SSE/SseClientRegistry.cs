using System.Collections.Concurrent;

namespace Transponder.Transports.SSE;

/// <summary>
/// Thread-safe registry for SSE client connections.
/// </summary>
public sealed class SseClientRegistry
{
    private readonly ConcurrentDictionary<string, SseClientConnection> _connections =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _streamIndex =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _groupIndex =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userIndex =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<SseClientConnection> GetAll()
        => _connections.Values.ToList();

    public bool TryGet(string connectionId, out SseClientConnection? connection)
        => _connections.TryGetValue(connectionId, out connection);

    public void Register(SseClientConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (!_connections.TryAdd(connection.Id, connection)) return;

        AddIndex(_streamIndex, connection.Streams, connection.Id);
        AddIndex(_groupIndex, connection.Groups, connection.Id);

        if (!string.IsNullOrWhiteSpace(connection.UserId))
            AddIndex(_userIndex, [connection.UserId], connection.Id);
    }

    public void Unregister(SseClientConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (!_connections.TryRemove(connection.Id, out _)) return;

        RemoveIndex(_streamIndex, connection.Streams, connection.Id);
        RemoveIndex(_groupIndex, connection.Groups, connection.Id);

        if (!string.IsNullOrWhiteSpace(connection.UserId))
            RemoveIndex(_userIndex, [connection.UserId], connection.Id);
    }

    public IReadOnlyList<SseClientConnection> ResolveTargets(SsePublishTargets targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        if (targets.Broadcast)
            return ExcludeConnections(_connections.Values, targets.ExcludedConnectionIds);

        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddConnections(resolved, targets.ConnectionIds);
        AddIndexedConnections(resolved, _userIndex, targets.Users);
        AddIndexedConnections(resolved, _groupIndex, targets.Groups);
        AddIndexedConnections(resolved, _streamIndex, targets.Streams);

        if (resolved.Count == 0 && targets.ExcludedConnectionIds.Count > 0)
            return ExcludeConnections(_connections.Values, targets.ExcludedConnectionIds);

        return CollectConnections(resolved, targets.ExcludedConnectionIds);
    }

    private void AddConnections(HashSet<string> resolved, IReadOnlyList<string> connectionIds)
    {
        foreach (string id in connectionIds)
            if (_connections.ContainsKey(id)) _ = resolved.Add(id);
    }

    private void AddIndexedConnections(
        HashSet<string> resolved,
        ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> index,
        IReadOnlyList<string> keys)
    {
        foreach (string key in keys)
        {
            if (!index.TryGetValue(key, out ConcurrentDictionary<string, byte>? ids)) continue;

            foreach (string id in ids.Keys) _ = resolved.Add(id);
        }
    }

    private IReadOnlyList<SseClientConnection> CollectConnections(
        HashSet<string> resolved,
        IReadOnlyList<string> excludedConnectionIds)
    {
        var excluded = new HashSet<string>(excludedConnectionIds, StringComparer.OrdinalIgnoreCase);
        var connections = new List<SseClientConnection>(resolved.Count);

        foreach (string id in resolved)
        {
            if (excluded.Contains(id)) continue;
            if (_connections.TryGetValue(id, out SseClientConnection? connection))
                connections.Add(connection);
        }

        return connections;
    }

    private static IReadOnlyList<SseClientConnection> ExcludeConnections(
        IEnumerable<SseClientConnection> connections,
        IReadOnlyList<string> excludedConnectionIds)
    {
        if (excludedConnectionIds.Count == 0) return connections.ToList();

        var excluded = new HashSet<string>(excludedConnectionIds, StringComparer.OrdinalIgnoreCase);
        return connections.Where(connection => !excluded.Contains(connection.Id)).ToList();
    }

    private static void AddIndex(
        ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> index,
        IReadOnlyList<string> keys,
        string connectionId)
    {
        foreach (string key in keys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            ConcurrentDictionary<string, byte> set = index.GetOrAdd(
                key,
                _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
            _ = set.TryAdd(connectionId, 0);
        }
    }

    private static void RemoveIndex(
        ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> index,
        IReadOnlyList<string> keys,
        string connectionId)
    {
        foreach (string key in keys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!index.TryGetValue(key, out ConcurrentDictionary<string, byte>? set)) continue;

            _ = set.TryRemove(connectionId, out _);
            if (set.IsEmpty) _ = index.TryRemove(key, out _);
        }
    }
}
