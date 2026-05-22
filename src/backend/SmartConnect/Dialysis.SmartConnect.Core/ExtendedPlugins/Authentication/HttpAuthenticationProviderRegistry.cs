using System.Collections.Frozen;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// Default <see cref="IHttpAuthenticationProviderRegistry"/>: indexes providers by <c>Kind</c>
/// (case-insensitive) at construction time and yields O(1) lookup per send.
/// </summary>
public sealed class HttpAuthenticationProviderRegistry : IHttpAuthenticationProviderRegistry
{
    private readonly FrozenDictionary<string, IHttpAuthenticationProvider> _byKind;

    public HttpAuthenticationProviderRegistry(IEnumerable<IHttpAuthenticationProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _byKind = providers.ToFrozenDictionary(p => p.Kind, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string kind, out IHttpAuthenticationProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(kind) && _byKind.TryGetValue(kind, out var found))
        {
            provider = found;
            return true;
        }

        provider = null!;
        return false;
    }
}
