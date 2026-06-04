using Dialysis.HIE.Core.Abstraction.Partners;

namespace Dialysis.HIE.Outbound.Partners;

public sealed class PartnerEndpointResolver : IPartnerEndpointResolver
{
    private readonly Dictionary<string, IPartnerEndpoint> _byId;
    public PartnerEndpointResolver(IEnumerable<IPartnerEndpoint> endpoints) => _byId = endpoints.ToDictionary(e => e.PartnerId, StringComparer.OrdinalIgnoreCase);

    public IPartnerEndpoint? Resolve(string partnerId) =>
        _byId.TryGetValue(partnerId, out var endpoint) ? endpoint : null;
}
