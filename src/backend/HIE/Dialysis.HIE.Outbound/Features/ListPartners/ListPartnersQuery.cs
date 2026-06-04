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
public sealed record PartnerStatusDto
{
    /// <summary>
    /// Wire shape for the partner-configuration strip on the HIE operator dashboard. Surfaces
    /// the partner id, the base URL the dispatcher routes to, whether a bearer token is on
    /// file, the configured request timeout, and a coarse "configured" flag — the
    /// pre-flight check the operator uses before turning the partner live.
    /// </summary>
    public PartnerStatusDto(string PartnerId,
        string BaseUrl,
        bool HasBearerToken,
        int TimeoutSeconds,
        bool IsConfigured)
    {
        this.PartnerId = PartnerId;
        this.BaseUrl = BaseUrl;
        this.HasBearerToken = HasBearerToken;
        this.TimeoutSeconds = TimeoutSeconds;
        this.IsConfigured = IsConfigured;
    }
    public string PartnerId { get; init; }
    public string BaseUrl { get; init; }
    public bool HasBearerToken { get; init; }
    public int TimeoutSeconds { get; init; }
    public bool IsConfigured { get; init; }
    public void Deconstruct(out string PartnerId, out string BaseUrl, out bool HasBearerToken, out int TimeoutSeconds, out bool IsConfigured)
    {
        PartnerId = this.PartnerId;
        BaseUrl = this.BaseUrl;
        HasBearerToken = this.HasBearerToken;
        TimeoutSeconds = this.TimeoutSeconds;
        IsConfigured = this.IsConfigured;
    }
}

public sealed record ListPartnersQuery : IQuery<IReadOnlyList<PartnerStatusDto>>, IPermissionedCommand
{
    public ListPartnersQuery()
    {
    }
    public string RequiredPermission => HiePermissions.PartnersAdminister;
}
