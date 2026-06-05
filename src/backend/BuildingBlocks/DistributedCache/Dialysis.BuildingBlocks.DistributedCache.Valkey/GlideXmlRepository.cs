using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Valkey.Glide;

namespace Dialysis.BuildingBlocks.DistributedCache.Valkey;

/// <summary>
/// ASP.NET Data Protection <see cref="IXmlRepository"/> backed by a Valkey list, so the key ring
/// is shared across horizontally-scaled replicas of a module. Equivalent to what
/// <c>PersistKeysToStackExchangeRedis</c> provided, but over Valkey GLIDE (the Microsoft package
/// binds the concrete StackExchange.Redis assembly and can't take a GLIDE connection).
///
/// Elements are appended to a single list key with <c>RPUSH</c> and read back with <c>LRANGE</c>.
/// The interface is synchronous and called rarely (ring load at startup, append on key creation),
/// so blocking on the GLIDE async calls is acceptable.
/// </summary>
internal sealed class GlideXmlRepository : IXmlRepository
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly string _key;

    public GlideXmlRepository(IConnectionMultiplexer multiplexer, string key)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _multiplexer = multiplexer;
        _key = key;
    }

    // GLIDE's IDatabase is async-only; IXmlRepository is synchronous. The bridge is safe here:
    // ASP.NET Core has no SynchronizationContext and GLIDE completes off-context, so there is no
    // sync-over-async deadlock. These methods are called rarely (ring load at startup, append on
    // key creation), not on a request hot path.
#pragma warning disable VSTHRD002
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var values = _multiplexer.GetDatabase()
            .ListRangeAsync(_key, 0, -1)
            .GetAwaiter()
            .GetResult();

        var elements = new List<XElement>(values.Length);
        foreach (var value in values)
        {
            if (!value.IsNull)
                elements.Add(XElement.Parse(value.ToString()));
        }
        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        _multiplexer.GetDatabase()
            .ListRightPushAsync(_key, element.ToString(SaveOptions.DisableFormatting))
            .GetAwaiter()
            .GetResult();
    }
#pragma warning restore VSTHRD002
}
