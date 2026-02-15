using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Default transport host resolver that prefers per-destination authority and falls back to scheme.
/// </summary>
public sealed class TransportHostProvider : ITransportHostProvider
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ITransportHost>> _hostsByAuthority;
    private readonly IReadOnlyDictionary<string, ITransportHost> _hostsByScheme;

    public TransportHostProvider(IEnumerable<ITransportHost> hosts)
    {
        ArgumentNullException.ThrowIfNull(hosts);

        var schemeMap = new Dictionary<string, ITransportHost>(StringComparer.OrdinalIgnoreCase);
        var authorityMap = new Dictionary<string, List<ITransportHost>>(StringComparer.OrdinalIgnoreCase);

        foreach (var host in hosts)
        {
            schemeMap[host.Address.Scheme] = host;

            var authorityKey = CreateAuthorityKey(host.Address);
            if (!authorityMap.TryGetValue(authorityKey, out var matches))
            {
                matches = [];
                authorityMap[authorityKey] = matches;
            }

            matches.Add(host);
        }

        _hostsByScheme = schemeMap;
        _hostsByAuthority = authorityMap.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<ITransportHost>)entry.Value);
    }

    public ITransportHost GetHost(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return !TryGetHost(address, out var host) || host is null
            ? throw new InvalidOperationException($"No transport host registered for scheme '{address.Scheme}'.")
            : host;
    }

    public bool TryGetHost(Uri address, out ITransportHost? host)
    {
        ArgumentNullException.ThrowIfNull(address);

        return address.IsAbsoluteUri && TryGetHostByAuthority(address, out host)
            ? true
            : _hostsByScheme.TryGetValue(address.Scheme, out host);
    }

    private bool TryGetHostByAuthority(Uri address, out ITransportHost? host)
    {
        var authorityKey = CreateAuthorityKey(address);

        if (!_hostsByAuthority.TryGetValue(authorityKey, out var matches) ||
            matches.Count == 0)
        {
            host = null;
            return false;
        }

        if (matches.Count == 1)
        {
            host = matches[0];
            return true;
        }

        host = SelectBestHost(matches, address) ?? matches[0];
        return true;
    }

    private static string CreateAuthorityKey(Uri address)
        => $"{address.Scheme}://{address.IdnHost}:{address.Port}";

    private static ITransportHost? SelectBestHost(
        IReadOnlyList<ITransportHost> matches,
        Uri destination)
    {
        ITransportHost? best = null;
        var bestLength = -1;

        foreach (var candidate in matches)
        {
            var baseUri = NormalizeBaseUri(candidate.Address);

            if (!baseUri.IsBaseOf(destination)) continue;

            var length = baseUri.AbsolutePath.Length;
            if (length <= bestLength) continue;

            best = candidate;
            bestLength = length;
        }

        return best;
    }

    private static Uri NormalizeBaseUri(Uri address)
    {
        var builder = new UriBuilder(address)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        var path = builder.Path;
        if (!path.EndsWith('/')) builder.Path = $"{path}/";

        return builder.Uri;
    }
}
