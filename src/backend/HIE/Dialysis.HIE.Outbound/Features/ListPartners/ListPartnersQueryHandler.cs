using Dialysis.CQRS.Queries;
using Dialysis.HIE.Outbound.Partners.Http;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.Features.ListPartners;

public sealed class ListPartnersQueryHandler(IOptionsMonitor<HiePartnersOptions> options)
    : IQueryHandler<ListPartnersQuery, IReadOnlyList<PartnerStatusDto>>
{
    public Task<IReadOnlyList<PartnerStatusDto>> HandleAsync(
        ListPartnersQuery _,
        CancellationToken cancellationToken)
    {
        var partners = options.CurrentValue.Partners;
        IReadOnlyList<PartnerStatusDto> result =
        [
            .. partners
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => new PartnerStatusDto(
                    PartnerId: p.Key,
                    BaseUrl: p.Value.BaseUrl,
                    HasBearerToken: !string.IsNullOrWhiteSpace(p.Value.BearerToken),
                    TimeoutSeconds: p.Value.TimeoutSeconds,
                    IsConfigured: !string.IsNullOrWhiteSpace(p.Value.BaseUrl)
                                  && Uri.TryCreate(p.Value.BaseUrl, UriKind.Absolute, out _))),
        ];
        return Task.FromResult(result);
    }
}
