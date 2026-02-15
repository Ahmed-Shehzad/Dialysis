using Dialysis.ApiClients;
using Dialysis.Analytics.Services;
using Hl7.Fhir.Model;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Descriptive;

public sealed class GetHypotensionRateQueryHandler : IQueryHandler<GetHypotensionRateQuery, GetHypotensionRateResult>
{
    private const string SystolicBpLoinc = "8480-6,85354-9";

    private readonly IFhirApi _fhirApi;
    private readonly IFhirBundleClient _bundleClient;
    private readonly string _fhirBaseUrl;

    public GetHypotensionRateQueryHandler(
        IFhirApi fhirApi,
        IFhirBundleClient bundleClient,
        Microsoft.Extensions.Options.IOptions<Analytics.Configuration.AnalyticsOptions> options)
    {
        _fhirApi = fhirApi;
        _bundleClient = bundleClient;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<GetHypotensionRateResult> HandleAsync(GetHypotensionRateQuery request, CancellationToken cancellationToken = default)
    {
        var (from, to) = ResolveDateRange(request.From, request.To);
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");

        var countBundle = await _fhirApi.SearchEncounters(
            date: ["ge" + fromStr, "le" + toStr],
            _summary: "count",
            cancellationToken: cancellationToken);
        var totalEncounters = countBundle.Total ?? 0;

        if (totalEncounters == 0)
            return new GetHypotensionRateResult("hypotension_rate", from, to, 0, 0, 0);

        var encounterIdsWithHypotension = new HashSet<string>();
        var nextUrl = $"{_fhirBaseUrl}Observation?code={SystolicBpLoinc}&date=ge{fromStr}&date=le{toStr}&_elements=encounter,valueQuantity&_count=100";

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Observation obs) continue;
                if (obs.Value is not Quantity q || !q.Value.HasValue || q.Value.Value >= 100) continue;
                var encRef = obs.Encounter?.Reference;
                if (string.IsNullOrEmpty(encRef)) continue;
                var encId = encRef.StartsWith("Encounter/", StringComparison.OrdinalIgnoreCase)
                    ? encRef["Encounter/".Length..]
                    : encRef;
                if (!string.IsNullOrEmpty(encId))
                    encounterIdsWithHypotension.Add(encId);
            }

            nextUrl = bundle.NextLink?.ToString() ?? "";
        }

        var rate = totalEncounters > 0
            ? Math.Round(100.0 * encounterIdsWithHypotension.Count / totalEncounters, 2)
            : 0;

        return new GetHypotensionRateResult(
            "hypotension_rate", from, to,
            totalEncounters, encounterIdsWithHypotension.Count, rate);
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
