using Dialysis.Hie.Core.Abstraction.Partners;

namespace Dialysis.Hie.Outbound.Partners;

public interface IPartnerEndpointResolver
{
    IPartnerEndpoint? Resolve(string partnerId);
}
