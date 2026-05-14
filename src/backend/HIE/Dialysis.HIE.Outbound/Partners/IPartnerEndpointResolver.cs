using Dialysis.HIE.Core.Abstraction.Partners;

namespace Dialysis.HIE.Outbound.Partners;

public interface IPartnerEndpointResolver
{
    IPartnerEndpoint? Resolve(string partnerId);
}
