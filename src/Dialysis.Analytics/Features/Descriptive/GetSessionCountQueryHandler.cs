using Dialysis.ApiClients;
using Dialysis.Analytics.Configuration;
using Intercessor.Abstractions;
using Microsoft.Extensions.Options;

namespace Dialysis.Analytics.Features.Descriptive;

public sealed class GetSessionCountQueryHandler : IQueryHandler<GetSessionCountQuery, GetSessionCountResult>
{
    private readonly IFhirApi _fhirApi;
    private readonly AnalyticsOptions _options;

    public GetSessionCountQueryHandler(IFhirApi fhirApi, IOptions<AnalyticsOptions> options)
    {
        _fhirApi = fhirApi;
        _options = options.Value;
    }

    public async Task<GetSessionCountResult> HandleAsync(GetSessionCountQuery request, CancellationToken cancellationToken = default)
    {
        var (from, to) = ResolveDateRange(request.From, request.To);
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");

        var bundle = await _fhirApi.SearchEncounters(
            date: ["ge" + fromStr, "le" + toStr],
            _summary: "count",
            cancellationToken: cancellationToken);

        var count = bundle.Total ?? 0;
        return new GetSessionCountResult("session_count", from, to, count);
    }

    private static (DateOnly from, DateOnly to) ResolveDateRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddDays(-30);
        var t = to ?? today;
        if (f > t) (f, t) = (t, f);
        return (f, t);
    }
}
