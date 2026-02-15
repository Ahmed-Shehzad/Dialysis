using System.Text;
using Dialysis.ApiClients;
using Dialysis.Registry.Configuration;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.Registry.Adapters;

/// <summary>CMS CROWNWeb adapter. Produces CSV for CMS-2728 (Medical Evidence Report) submission.</summary>
public sealed class CrownWebAdapter : IRegistryAdapter
{
    public string Name => "CROWNWeb";

    private readonly IFhirApi _fhirApi;
    private readonly IFhirBundleClient _bundleClient;
    private readonly string _fhirBaseUrl;

    public CrownWebAdapter(IFhirApi fhirApi, IFhirBundleClient bundleClient, IOptions<RegistryOptions> options)
    {
        _fhirApi = fhirApi;
        _bundleClient = bundleClient;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<RegistryExportResult> ExportAsync(RegistryExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromStr = request.From.ToString("yyyy-MM-dd");
            var toStr = request.To.ToString("yyyy-MM-dd");
            var url = $"{_fhirBaseUrl}Encounter?date=ge{fromStr}&date=le{toStr}&_elements=id,subject,period,status,class&_count=500";

            var sb = new StringBuilder();
            sb.AppendLine("CROWNWeb_CMS2728_Export");
            sb.AppendLine("PatientId,EncounterId,SessionDate,Status,TreatmentType,ExportDate");

            var nextUrl = url;
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is not Encounter enc) continue;
                    var patientId = enc.Subject?.Reference?.Split('/').LastOrDefault() ?? "";
                    var date = enc.Period?.StartElement?.ToString() ?? "";
                    var status = enc.Status.ToString();
                    var treatmentType = enc.Class?.Code ?? "AMB";
                    sb.AppendLine($"{EscapeCsv(patientId)},{EscapeCsv(enc.Id ?? "")},{EscapeCsv(date)},{status},{treatmentType},{request.From:yyyy-MM-dd}");
                }
                nextUrl = bundle.NextLink?.ToString() ?? "";
            }

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            ms.Position = 0;
            var filename = $"crownweb-export-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.csv";
            return new RegistryExportResult(true, Name, ms, filename, null);
        }
        catch (Exception ex)
        {
            return new RegistryExportResult(false, Name, null, null, ex.Message);
        }
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
