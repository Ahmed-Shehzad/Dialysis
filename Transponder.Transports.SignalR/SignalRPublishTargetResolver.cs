using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SignalR;

internal static class SignalRPublishTargetResolver
{
    public static SignalRPublishTargets Resolve(ITransportMessage message)
        => Resolve(message, destination: null, allowDefaultBroadcast: true);

    public static SignalRPublishTargets Resolve(
        ITransportMessage message,
        Uri? destination,
        bool allowDefaultBroadcast)
    {
        ArgumentNullException.ThrowIfNull(message);

        IReadOnlyDictionary<string, object?> headers = message.Headers;
        bool broadcast = TryGetBoolean(headers, TransponderSignalRHeaders.Broadcast);

        List<string> connectionIds = GetValues(headers, TransponderSignalRHeaders.ConnectionId, TransponderSignalRHeaders.ConnectionIds);
        List<string> excludedConnectionIds = GetValues(headers, TransponderSignalRHeaders.ExcludeConnectionId, TransponderSignalRHeaders.ExcludeConnectionIds);
        List<string> groups = GetValues(headers, TransponderSignalRHeaders.Group, TransponderSignalRHeaders.Groups);
        List<string> users = GetValues(headers, TransponderSignalRHeaders.User, TransponderSignalRHeaders.Users);

        if (destination is not null)
            ApplyDestination(destination, connectionIds, groups, users, ref broadcast);

        bool hasTargets = connectionIds.Count > 0 || groups.Count > 0 || users.Count > 0;
        if (!broadcast && !hasTargets && allowDefaultBroadcast) broadcast = true;

        return new SignalRPublishTargets(
            broadcast,
            Deduplicate(connectionIds),
            Deduplicate(excludedConnectionIds),
            Deduplicate(groups),
            Deduplicate(users));
    }

    private static bool TryGetBoolean(IReadOnlyDictionary<string, object?> headers, string key)
    {
        if (!headers.TryGetValue(key, out object? value) || value is null) return false;

        if (value is bool flag) return flag;

        return bool.TryParse(value.ToString(), out bool parsed) && parsed;
    }

    private static List<string> GetValues(IReadOnlyDictionary<string, object?> headers, params string[] keys)
    {
        var values = new List<string>();

        foreach (string key in keys)
        {
            if (!headers.TryGetValue(key, out object? value) || value is null) continue;

            switch (value)
            {
                case string text:
                    AddDelimited(values, text);
                    break;
                case IEnumerable<string> items:
                    foreach (string item in items) AddDelimited(values, item);
                    break;
                case IEnumerable<object> objects:
                    foreach (object item in objects) AddDelimited(values, item?.ToString());
                    break;
                default:
                    AddDelimited(values, value.ToString());
                    break;
            }
        }

        return values.Count == 0 ? values : values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddDelimited(List<string> values, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        string[] segments = raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IEnumerable<string> filteredSegments = segments.Where(segment => !string.IsNullOrWhiteSpace(segment));
        values.AddRange(filteredSegments);
    }

    private static List<string> Deduplicate(List<string> values)
        => values.Count == 0 ? values : values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static void ApplyDestination(
        Uri destination,
        List<string> connectionIds,
        List<string> groups,
        List<string> users,
        ref bool broadcast)
    {
        if (!TryResolveDestination(destination, out string? targetKey, out List<string> values, out bool isBroadcast))
            return;

        if (isBroadcast)
        {
            broadcast = true;
            return;
        }

        if (values.Count == 0) return;

        if (IsConnectionTarget(targetKey)) connectionIds.AddRange(values);
        else if (IsGroupTarget(targetKey)) groups.AddRange(values);
        else if (IsUserTarget(targetKey)) users.AddRange(values);
    }

    private static bool TryResolveDestination(
        Uri destination,
        out string? targetKey,
        out List<string> values,
        out bool isBroadcast)
    {
        targetKey = null;
        values = [];
        isBroadcast = false;

        string host = destination.IsAbsoluteUri ? destination.Host : string.Empty;
        string path = destination.IsAbsoluteUri ? destination.AbsolutePath : destination.ToString();

        string? candidate = null;
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (IsTargetKey(host))
        {
            candidate = host;
            foreach (string segment in segments) AddDelimited(values, segment);
        }
        else if (segments.Length > 0 && IsTargetKey(segments[0]))
        {
            candidate = segments[0];
            for (int i = 1; i < segments.Length; i++) AddDelimited(values, segments[i]);
        }

        if (candidate is null) return false;

        if (IsBroadcastTarget(candidate))
        {
            isBroadcast = true;
            targetKey = candidate;
            return true;
        }

        if (values.Count == 0) return false;

        targetKey = candidate;
        return true;
    }

    private static bool IsTargetKey(string? value)
        => IsConnectionTarget(value) || IsGroupTarget(value) || IsUserTarget(value) || IsBroadcastTarget(value);

    private static bool IsConnectionTarget(string? value)
        => string.Equals(value, "connection", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "connections", StringComparison.OrdinalIgnoreCase);

    private static bool IsGroupTarget(string? value)
        => string.Equals(value, "group", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "groups", StringComparison.OrdinalIgnoreCase);

    private static bool IsUserTarget(string? value)
        => string.Equals(value, "user", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "users", StringComparison.OrdinalIgnoreCase);

    private static bool IsBroadcastTarget(string? value)
        => string.Equals(value, "broadcast", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "all", StringComparison.OrdinalIgnoreCase);
}
