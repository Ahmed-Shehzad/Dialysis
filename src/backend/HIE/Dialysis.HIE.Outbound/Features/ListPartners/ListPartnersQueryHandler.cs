using Dialysis.CQRS.Queries;
using Dialysis.HIE.Outbound.Partners.Http;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.Features.ListPartners;

public sealed class ListPartnersQueryHandler : IQueryHandler<ListPartnersQuery, IReadOnlyList<PartnerStatusDto>>
{
    private readonly IOptionsMonitor<HiePartnersOptions> _options;
    public ListPartnersQueryHandler(IOptionsMonitor<HiePartnersOptions> options) => _options = options;
    public Task<IReadOnlyList<PartnerStatusDto>> HandleAsync(
        ListPartnersQuery query,
        CancellationToken cancellationToken)
    {
        var partners = _options.CurrentValue.Partners;
        IReadOnlyList<PartnerStatusDto> result =
        [
            .. partners
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => new PartnerStatusDto(
                    PartnerId: p.Key,
                    BaseUrl: p.Value.BaseUrl,
                    HasBearerToken: !string.IsNullOrWhiteSpace(p.Value.BearerToken),
                    TimeoutSeconds: p.Value.TimeoutSeconds,
                    IsConfigured: IsValidAbsoluteUri(p.Value.BaseUrl))),
        ];
        return Task.FromResult(result);
    }

    private static bool IsValidAbsoluteUri(string? baseUrl) =>
        !string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out _);
}
