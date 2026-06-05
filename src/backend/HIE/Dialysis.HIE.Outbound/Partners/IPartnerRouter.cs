using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.Partners;

/// <summary>
/// Resolves which partner(s) a disclosure for a patient is routed to — replacing the old single
/// hard-coded <c>DefaultPartnerId</c>. Event-driven pushes broadcast to every resolved partner;
/// referrals target an explicit destination.
/// </summary>
public interface IPartnerRouter
{
    /// <summary>Partners a disclosure of <paramref name="scope"/> for <paramref name="patientId"/> goes to.</summary>
    IReadOnlyList<string> ResolvePartners(Guid patientId, string scope);
}

/// <summary>
/// Configuration-driven router: returns <c>Hie:Outbound:RoutingPartners</c> when set, else the
/// single <c>DefaultPartnerId</c> (back-compatible). A patient-network-aware strategy slots in here
/// later without touching call sites.
/// </summary>
public sealed class ConfiguredPartnerRouter : IPartnerRouter
{
    private readonly OutboundOptions _options;
    public ConfiguredPartnerRouter(IOptions<OutboundOptions> options) => _options = options.Value;

    public IReadOnlyList<string> ResolvePartners(Guid patientId, string scope) =>
        _options.RoutingPartners.Count > 0
            ? _options.RoutingPartners
            : [_options.DefaultPartnerId];
}
