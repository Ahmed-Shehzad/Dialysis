using Dialysis.Hie.Core.Abstraction.Partners;

namespace Dialysis.Hie.Outbound.Partners;

public sealed class PartnerEndpointResolver(IEnumerable<IPartnerEndpoint> endpoints) : IPartnerEndpointResolver
{
    private readonly Dictionary<string, IPartnerEndpoint> _byId = endpoints.ToDictionary(e => e.PartnerId, StringComparer.OrdinalIgnoreCase);

    public IPartnerEndpoint? Resolve(string partnerId) =>
        _byId.TryGetValue(partnerId, out var endpoint) ? endpoint : null;
}
