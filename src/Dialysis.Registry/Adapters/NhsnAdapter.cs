using System.Runtime.CompilerServices;
using System.Text;
using Dialysis.ApiClients;
using Dialysis.Registry.Configuration;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.Registry.Adapters;

/// <summary>CDC NHSN Dialysis Event adapter. Exports infection events and vascular access data for NHSN surveillance.</summary>
public sealed class NhsnAdapter : IRegistryAdapter
{
    public string Name => "NHSN";

    private readonly IFhirBundleClient _bundleClient;
    private readonly string _fhirBaseUrl;

    public NhsnAdapter(IFhirBundleClient bundleClient, IOptions<RegistryOptions> options)
    {
        _bundleClient = bundleClient;
        _fhirBaseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<RegistryExportResult> ExportAsync(RegistryExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = request.TenantId ?? "default";
            var sb = new StringBuilder();
            sb.AppendLine("NHSN_DialysisEvent_Export");
            sb.AppendLine("FacilityId,PatientId,EventDate,EventType,VascularAccessType,ResourceType,ResourceId,Code,EncounterId,ExportDate");

            var fromStr = request.From.ToString("yyyy-MM-dd");
            var toStr = request.To.ToString("yyyy-MM-dd");

            await foreach (var row in FetchConditionEventsAsync(fromStr, toStr, facilityId, request.From, cancellationToken))
                sb.AppendLine(row);
            await foreach (var row in FetchProcedureEventsAsync(fromStr, toStr, facilityId, request.From, cancellationToken))
                sb.AppendLine(row);

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            ms.Position = 0;
            var filename = $"nhsn-export-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.csv";
            return new RegistryExportResult(true, Name, ms, filename, null);
        }
        catch (Exception ex)
        {
            return new RegistryExportResult(false, Name, null, null, ex.Message);
        }
    }

    private async IAsyncEnumerable<string> FetchConditionEventsAsync(
        string fromStr, string toStr, string facilityId, DateOnly exportDate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var url = $"{_fhirBaseUrl}Condition?onset-date=ge{fromStr}&onset-date=le{toStr}&_elements=id,code,subject,onsetDateTime,onsetPeriod,encounter&_count=500";
        var nextUrl = url;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Condition cond) continue;
                var patientId = ExtractPatientId(cond.Subject?.Reference);
                var eventDate = ExtractDate(cond.Onset as DataType);
                var (eventType, code) = MapConditionCode(cond);
                var encounterId = ExtractReferenceId(cond.Encounter?.Reference);
                yield return CsvRow(facilityId, patientId, eventDate, eventType, "", "Condition", cond.Id ?? "", code, encounterId, exportDate);
            }
            nextUrl = bundle.NextLink?.ToString() ?? "";
        }
    }

    private async IAsyncEnumerable<string> FetchProcedureEventsAsync(
        string fromStr, string toStr, string facilityId, DateOnly exportDate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var url = $"{_fhirBaseUrl}Procedure?date=ge{fromStr}&date=le{toStr}&_elements=id,code,subject,performedDateTime,performedPeriod,encounter&_count=500";
        var nextUrl = url;

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var bundle = await _bundleClient.GetBundleAsync(nextUrl, cancellationToken);
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Procedure proc) continue;
                var accessType = MapProcedureToVascularAccessType(proc);
                if (string.IsNullOrEmpty(accessType)) continue;
                var patientId = ExtractPatientId(proc.Subject?.Reference);
                var eventDate = ExtractProcedureDate(proc);
                var code = proc.Code?.Coding?.FirstOrDefault()?.Code ?? "";
                var encounterId = ExtractReferenceId(proc.Encounter?.Reference);
                yield return CsvRow(facilityId, patientId, eventDate, "VascularAccessProcedure", accessType, "Procedure", proc.Id ?? "", code, encounterId, exportDate);
            }
            nextUrl = bundle.NextLink?.ToString() ?? "";
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

    private static string ExtractDate(DataType? onset)
    {
        if (onset == null) return "";
        if (onset is FhirDateTime dt && !string.IsNullOrEmpty(dt.Value))
            return dt.Value;
        if (onset is Period p && !string.IsNullOrEmpty(p.Start))
            return p.Start;
        return "";
    }

    private static string ExtractProcedureDate(Procedure proc)
    {
        if (proc.Performed is FhirDateTime dt && !string.IsNullOrEmpty(dt.Value))
            return dt.Value;
        if (proc.Performed is Period p && !string.IsNullOrEmpty(p.Start))
            return p.Start;
        return "";
    }

    private static (string EventType, string Code) MapConditionCode(Condition cond)
    {
        var code = cond.Code?.Coding?.FirstOrDefault()?.Code ?? "";
        var display = cond.Code?.Coding?.FirstOrDefault()?.Display ?? "";
        var system = cond.Code?.Coding?.FirstOrDefault()?.System ?? "";

        if (system.Contains("icd10", StringComparison.OrdinalIgnoreCase) || code.StartsWith("A41", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("R78", StringComparison.OrdinalIgnoreCase) || display.Contains("sepsis", StringComparison.OrdinalIgnoreCase) ||
            display.Contains("bacteremia", StringComparison.OrdinalIgnoreCase))
            return ("BSI", code);
        if (display.Contains("vascular access", StringComparison.OrdinalIgnoreCase) || display.Contains("access site", StringComparison.OrdinalIgnoreCase))
            return ("VascularAccessInfection", code);
        if (display.Contains("IV", StringComparison.OrdinalIgnoreCase) || display.Contains("intravenous", StringComparison.OrdinalIgnoreCase))
            return ("IVSiteInfection", code);
        return ("Infection", code);
    }

    private static string MapProcedureToVascularAccessType(Procedure proc)
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

    private static string CsvRow(string facilityId, string patientId, string eventDate, string eventType,
        string vascularAccessType, string resourceType, string resourceId, string code,
        string encounterId, DateOnly exportDate)
    {
        return $"{Escape(facilityId)},{Escape(patientId)},{Escape(eventDate)},{Escape(eventType)},{Escape(vascularAccessType)}," +
               $"{Escape(resourceType)},{Escape(resourceId)},{Escape(code)},{Escape(encounterId)},{exportDate:yyyy-MM-dd}";
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
