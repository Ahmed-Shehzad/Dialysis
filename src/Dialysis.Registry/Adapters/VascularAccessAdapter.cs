using System.Text;
using Dialysis.ApiClients;
using Dialysis.Registry.Configuration;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.Registry.Adapters;

/// <summary>Vascular access registry adapter. Exports fistula, graft, and catheter procedures.</summary>
public sealed class VascularAccessAdapter : IRegistryAdapter
{
    public string Name => "VascularAccess";

    private readonly IFhirBundleClient _bundleClient;
    private readonly string _fhirBaseUrl;

    public VascularAccessAdapter(IFhirBundleClient bundleClient, IOptions<RegistryOptions> options)
    {
        _bundleClient = bundleClient;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<RegistryExportResult> ExportAsync(RegistryExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("VascularAccess_Export");
            sb.AppendLine("PatientId,ProcedureId,ProcedureDate,ProcedureType,Laterality,Status,EncounterId,ExportDate");

            var fromStr = request.From.ToString("yyyy-MM-dd");
            var toStr = request.To.ToString("yyyy-MM-dd");
            var url = $"{_fhirBaseUrl}Procedure?date=ge{fromStr}&date=le{toStr}&_elements=id,code,subject,performedDateTime,performedPeriod,bodySite,encounter,status&_count=500";

            var nextUrl = url;
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is not Procedure proc) continue;
                    var procedureType = MapProcedureType(proc);
                    if (string.IsNullOrEmpty(procedureType)) continue;

                    var patientId = ExtractPatientId(proc.Subject?.Reference);
                    var procedureDate = ExtractProcedureDate(proc);
                    var laterality = ExtractLaterality(proc);
                    var status = proc.Status?.ToString() ?? "";
                    var encounterId = ExtractReferenceId(proc.Encounter?.Reference);

                    sb.AppendLine($"{Escape(patientId)},{Escape(proc.Id ?? "")},{Escape(procedureDate)},{Escape(procedureType)}," +
                        $"{Escape(laterality)},{Escape(status)},{Escape(encounterId)},{request.From:yyyy-MM-dd}");
                }
                nextUrl = bundle.NextLink?.ToString() ?? "";
            }

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            ms.Position = 0;
            var filename = $"vascular-access-export-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.csv";
            return new RegistryExportResult(true, Name, ms, filename, null);
        }
        catch (Exception ex)
        {
            return new RegistryExportResult(false, Name, null, null, ex.Message);
        }
    }

    private static string ExtractPatientId(string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return "";
        var parts = reference.Split('/');
        return parts.Length > 1 ? parts[^1] : reference;
    }

    private static string ExtractReferenceId(string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return "";
        var parts = reference.Split('/');
        return parts.Length > 1 ? parts[^1] : "";
    }

    private static string ExtractProcedureDate(Procedure proc)
    {
        if (proc.Performed is FhirDateTime dt && !string.IsNullOrEmpty(dt.Value))
            return dt.Value;
        if (proc.Performed is Period p && !string.IsNullOrEmpty(p.Start))
            return p.Start;
        return "";
    }

    private static string ExtractLaterality(Procedure proc)
    {
        var display = proc.BodySite?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Display ?? "";
        if (display.Contains("left", StringComparison.OrdinalIgnoreCase)) return "Left";
        if (display.Contains("right", StringComparison.OrdinalIgnoreCase)) return "Right";
        if (display.Contains("bilateral", StringComparison.OrdinalIgnoreCase)) return "Bilateral";
        return "";
    }

    private static string MapProcedureType(Procedure proc)
    {
        var code = proc.Code?.Coding?.FirstOrDefault()?.Code ?? "";
        var display = proc.Code?.Coding?.FirstOrDefault()?.Display ?? "";

        if (code is "36821" or "36818" or "36819" or "36820" || display.Contains("fistula", StringComparison.OrdinalIgnoreCase))
            return "Fistula";
        if (code is "36831" or "36832" or "36833" || display.Contains("graft", StringComparison.OrdinalIgnoreCase))
            return "Graft";
        if (code is "36558" or "36561" || display.Contains("tunneled", StringComparison.OrdinalIgnoreCase))
            return "TunneledCatheter";
        if (code is "36555" or "36556" || display.Contains("catheter", StringComparison.OrdinalIgnoreCase))
            return "Catheter";
        return "";
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
