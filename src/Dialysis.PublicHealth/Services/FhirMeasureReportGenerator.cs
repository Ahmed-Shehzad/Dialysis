using System.Text;
using Dialysis.ApiClients;
using Dialysis.PublicHealth.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.PublicHealth.Services;

/// <summary>Generates FHIR MeasureReport format for public health submission. Stub implementation.</summary>
public sealed class FhirMeasureReportGenerator : IReportGenerator
{
    public string Format => "fhir-measure-report";

    private readonly IFhirApi _fhirApi;
    private readonly string _fhirBaseUrl;

    public FhirMeasureReportGenerator(IFhirApi fhirApi, IOptions<PublicHealthOptions> options)
    {
        _fhirApi = fhirApi;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = await _fhirApi.SearchEncounters(
                date: ["ge" + request.From.ToString("yyyy-MM-dd"), "le" + request.To.ToString("yyyy-MM-dd")],
                _summary: "count",
                cancellationToken: cancellationToken);

            var count = bundle.Total ?? 0;
            var json = $$"""
                {
                  "resourceType": "MeasureReport",
                  "status": "complete",
                  "type": "summary",
                  "measure": "http://example.org/measures/dialysis-sessions",
                  "period": {
                    "start": "{{request.From:yyyy-MM-dd}}",
                    "end": "{{request.To:yyyy-MM-dd}}"
                  },
                  "group": [{
                    "count": {{count}}
                  }]
                }
                """;

            var content = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var filename = $"measure-report-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.json";
            return new ReportResult(true, Format, content, filename, null);
        }
        catch (Exception ex)
        {
            return new ReportResult(false, Format, null, null, ex.Message);
        }
    }
}
