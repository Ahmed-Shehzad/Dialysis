using System.Text;
using Task = System.Threading.Tasks.Task;
using Dialysis.ApiClients;
using Dialysis.Analytics.Configuration;
using Dialysis.Analytics.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Intercessor.Abstractions;
using Microsoft.Extensions.Options;

namespace Dialysis.Analytics.Features.Export;

public sealed class ExportCommandHandler : ICommandHandler<ExportCommand>
{
    private readonly IFhirApi _fhirApi;
    private readonly IFhirBundleClient _bundleClient;
    private readonly IAnalyticsAuditRecorder _audit;
    private readonly string _fhirBaseUrl;

    public ExportCommandHandler(
        IFhirApi fhirApi,
        IFhirBundleClient bundleClient,
        IAnalyticsAuditRecorder audit,
        IOptions<AnalyticsOptions> options)
    {
        _fhirApi = fhirApi;
        _bundleClient = bundleClient;
        _audit = audit;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task HandleAsync(ExportCommand request, CancellationToken cancellationToken = default)
    {
        var (resourceType, from, to, format, output) = request;
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");

        var dateParam = resourceType switch
        {
            "Encounter" => $"date=ge{fromStr}&date=le{toStr}",
            "Observation" => $"date=ge{fromStr}&date=le{toStr}",
            "Patient" => "",
            _ => ""
        };

        var baseQuery = dateParam.Length > 0
            ? $"{_fhirBaseUrl}{resourceType}?{dateParam}&_count=100"
            : $"{_fhirBaseUrl}{resourceType}?_count=100";
        var nextUrl = baseQuery;

        if (format == ExportFormat.Csv)
        {
            await WriteCsvHeaderAsync(resourceType, output, cancellationToken);
        }

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource == null) continue;
                if (format == ExportFormat.NdJson)
                {
                    var resourceJson = new FhirJsonSerializer().SerializeToString(entry.Resource);
                    var line = resourceJson + "\n";
                    await output.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
                }
                else
                {
                    await WriteCsvRowAsync(entry.Resource, resourceType, output, cancellationToken);
                }
            }

            nextUrl = bundle.NextLink?.ToString() ?? "";
        }

        await _audit.RecordAsync("Export", $"{resourceType}-{from:yyyyMMdd}-{to:yyyyMMdd}", "read", outcome: "0", cancellationToken: cancellationToken);
    }

    private static async Task WriteCsvHeaderAsync(string resourceType, Stream output, CancellationToken ct)
    {
        var header = resourceType switch
        {
            "Patient" => "id,identifier,name,birthDate,gender\n",
            "Encounter" => "id,status,subject,periodStart,periodEnd\n",
            "Observation" => "id,subject,encounter,code,value,effective\n",
            _ => "id\n"
        };
        await output.WriteAsync(Encoding.UTF8.GetBytes(header), ct);
    }

    private static async Task WriteCsvRowAsync(Resource resource, string resourceType, Stream output, CancellationToken ct)
    {
        var row = resourceType switch
        {
            "Patient" when resource is Patient p =>
                $"{Escape(p.Id)},{Escape(p.Identifier?.FirstOrDefault()?.Value)},{Escape(p.Name?.FirstOrDefault()?.ToString())},{Escape(p.BirthDate)},{Escape(p.Gender?.ToString())}\n",
            "Encounter" when resource is Encounter e =>
                $"{Escape(e.Id)},{Escape(e.Status?.ToString())},{Escape(e.Subject?.Reference)},{Escape(e.Period?.Start)},{Escape(e.Period?.End)}\n",
            "Observation" when resource is Observation o =>
                $"{Escape(o.Id)},{Escape(o.Subject?.Reference)},{Escape(o.Encounter?.Reference)},{Escape(o.Code?.Coding?.FirstOrDefault()?.Code)},{Escape(o.Value?.ToString())},{Escape(o.Effective?.ToString())}\n",
            _ => $"{Escape(resource.Id)}\n"
        };
        await output.WriteAsync(Encoding.UTF8.GetBytes(row), ct);
    }

    private static string Escape(string? v) => v == null ? "" : "\"" + v.Replace("\"", "\"\"") + "\"";
}
