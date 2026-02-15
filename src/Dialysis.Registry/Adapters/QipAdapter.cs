using System.Text;
using Dialysis.ApiClients;
using Dialysis.Registry.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.Registry.Adapters;

/// <summary>QIP (Quality Incentive Program) adapter. Produces summary CSV/JSON for quality reporting.</summary>
public sealed class QipAdapter : IRegistryAdapter
{
    public string Name => "QIP";

    private readonly IFhirApi _fhirApi;
    private readonly string _fhirBaseUrl;

    public QipAdapter(IFhirApi fhirApi, IOptions<RegistryOptions> options)
    {
        _fhirApi = fhirApi;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<RegistryExportResult> ExportAsync(RegistryExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromStr = request.From.ToString("yyyy-MM-dd");
            var toStr = request.To.ToString("yyyy-MM-dd");

            var bundle = await _fhirApi.SearchEncounters(
                date: ["ge" + fromStr, "le" + toStr],
                _summary: "count",
                cancellationToken: cancellationToken);

            var count = bundle.Total ?? 0;
            var csv = $"Period,From,To,SessionCount\nQIP,{request.From:yyyy-MM-dd},{request.To:yyyy-MM-dd},{count}\n";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            ms.Position = 0;
            var filename = $"qip-export-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.csv";
            return new RegistryExportResult(true, Name, ms, filename, null);
        }
        catch (Exception ex)
        {
            return new RegistryExportResult(false, Name, null, null, ex.Message);
        }
    }
}
