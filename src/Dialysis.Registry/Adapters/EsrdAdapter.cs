using System.Text;
using Dialysis.ApiClients;
using Dialysis.Registry.Configuration;
using Dialysis.Registry.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;

namespace Dialysis.Registry.Adapters;

/// <summary>ESRD (End-Stage Renal Disease) CMS registry adapter. Produces NDJSON or HL7 v2 (ORU^R01) for registry submission.</summary>
public sealed class EsrdAdapter : IRegistryAdapter
{
    public string Name => "ESRD";

    private readonly IFhirApi _fhirApi;
    private readonly IFhirBundleClient _bundleClient;
    private readonly string _fhirBaseUrl;

    public EsrdAdapter(IFhirApi fhirApi, IFhirBundleClient bundleClient, IOptions<RegistryOptions> options)
    {
        _fhirApi = fhirApi;
        _bundleClient = bundleClient;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<RegistryExportResult> ExportAsync(RegistryExportRequest request, CancellationToken cancellationToken = default)
    {
        var useHl7V2 = string.Equals(request.OutputFormat, RegistryOutputFormat.Hl7V2, StringComparison.OrdinalIgnoreCase);
        return useHl7V2
            ? await ExportHl7V2Async(request, cancellationToken)
            : await ExportNdJsonAsync(request, cancellationToken);
    }

    private async Task<RegistryExportResult> ExportNdJsonAsync(RegistryExportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var encounters = await FetchEncountersAsync(request, cancellationToken);
            var ms = new MemoryStream();
            var serializer = new FhirJsonSerializer();

            foreach (var enc in encounters)
            {
                var json = serializer.SerializeToString(enc);
                await ms.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"), cancellationToken);
            }

            ms.Position = 0;
            var filename = $"esrd-export-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.ndjson";
            return new RegistryExportResult(true, Name, ms, filename, null);
        }
        catch (Exception ex)
        {
            return new RegistryExportResult(false, Name, null, null, ex.Message);
        }
    }

    private async Task<RegistryExportResult> ExportHl7V2Async(RegistryExportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var encounters = await FetchEncountersAsync(request, cancellationToken);
            var ms = new MemoryStream();
            var sendingApp = "Dialysis.Registry";
            var sendingFacility = "PDMS";

            foreach (var enc in encounters)
            {
                var patientId = enc.Subject?.Reference?.Split('/').LastOrDefault() ?? enc.Id ?? "unknown";
                var periodStart = enc.Period?.Start ?? DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

                var segments = new List<Hl7Segment>
                {
                    new("PID", ["1", "", patientId, "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", patientId]),
                    new("OBR", ["1", "", enc.Id ?? "", "Encounter", "", periodStart, "", "", "", "", "", "", "", "", "", "", periodStart, "Dialysis Session", "", ""]),
                    new("OBX", ["1", "ST", "period.start", "Encounter period start", "LN", periodStart, "", "", "F", "", "", "", "", ""]),
                };

                var msg = Hl7V2Formatter.BuildOruMessage(sendingApp, sendingFacility, DateTime.UtcNow, segments);
                await ms.WriteAsync(Encoding.UTF8.GetBytes(msg + "\r\n"), cancellationToken);
            }

            ms.Position = 0;
            var filename = $"esrd-export-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.hl7";
            return new RegistryExportResult(true, Name, ms, filename, null);
        }
        catch (Exception ex)
        {
            return new RegistryExportResult(false, Name, null, null, ex.Message);
        }
    }

    private async Task<List<Encounter>> FetchEncountersAsync(RegistryExportRequest request, CancellationToken cancellationToken)
    {
        var fromStr = request.From.ToString("yyyy-MM-dd");
        var toStr = request.To.ToString("yyyy-MM-dd");
        var url = $"{_fhirBaseUrl}Encounter?date=ge{fromStr}&date=le{toStr}&_elements=id,subject,period,status&_count=500";
        var list = new List<Encounter>();
        var nextUrl = url;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is Encounter enc)
                    list.Add(enc);
            }
            nextUrl = bundle.NextLink?.ToString() ?? "";
        }

        return list;
    }
}
