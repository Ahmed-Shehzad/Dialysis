using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Outbound.Features.ListPartners;

/// <summary>
/// Wire shape for the partner-configuration strip on the HIE operator dashboard. Surfaces
/// the partner id, the base URL the dispatcher routes to, whether a bearer token is on
/// file, the configured request timeout, and a coarse "configured" flag — the
/// pre-flight check the operator uses before turning the partner live.
/// </summary>
public sealed record PartnerStatusDto(
    string PartnerId,
    string BaseUrl,
    bool HasBearerToken,
    int TimeoutSeconds,
    bool IsConfigured);

public sealed record ListPartnersQuery()
    : IQuery<IReadOnlyList<PartnerStatusDto>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.PartnersAdminister;
}
