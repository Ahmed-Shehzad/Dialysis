using System.Text;
using System.Text.Json;
using Dialysis.PublicHealth.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.PublicHealth.Services;

/// <summary>Converts FHIR MeasureReport JSON to HL7 v2 format for PH endpoints that expect HL7.</summary>
public interface IMeasureReportToHl7V2Converter
{
    Task<Stream> ConvertAsync(Stream fhirJsonStream, CancellationToken cancellationToken = default);
}

public sealed class MeasureReportToHl7V2Converter : IMeasureReportToHl7V2Converter
{
    private static readonly char[] Hl7EscapeChars = ['|', '^', '~', '\\', '&'];

    private readonly PublicHealthOptions _options;

    public MeasureReportToHl7V2Converter(IOptions<PublicHealthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Stream> ConvertAsync(Stream fhirJsonStream, CancellationToken cancellationToken = default)
    {
        var report = await JsonSerializer.DeserializeAsync<MeasureReportDto>(fhirJsonStream, cancellationToken: cancellationToken);
        if (report == null)
            throw new InvalidOperationException("Invalid FHIR MeasureReport JSON");

        var msg = BuildOruFromMeasureReport(report);
        return new MemoryStream(Encoding.UTF8.GetBytes(msg));
    }

    private string BuildOruFromMeasureReport(MeasureReportDto report)
    {
        var sb = new StringBuilder();
        var msgTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var controlId = Guid.NewGuid().ToString("N")[..20];
        var sendingApp = _options.Hl7SendingApp ?? "Dialysis.PublicHealth";
        var sendingFacility = _options.Hl7SendingFacility ?? "PDMS";

        sb.Append("MSH|^~\\&|").Append(Escape(sendingApp)).Append("|").Append(Escape(sendingFacility))
            .Append("|||").Append(msgTime).Append("||ORU^R01^ORU_R01|").Append(controlId)
            .Append("|P|2.5\r");

        var measure = report.Measure ?? "dialysis-sessions";
        var periodStart = report.Period?.Start ?? "";
        var periodEnd = report.Period?.End ?? "";
        var obrId = report.Id ?? Guid.NewGuid().ToString("N")[..20];
        var count = report.Group?.FirstOrDefault()?.Count ?? 0;

        sb.Append("OBR|1|").Append(Escape(obrId)).Append("|")
            .Append(Escape(measure)).Append("|MeasureReport|")
            .Append(Escape(periodStart)).Append("|").Append(Escape(periodEnd))
            .Append("|||||||||||||||F\r");

        sb.Append("OBX|1|NM|count^Count^LOINC|").Append(count).Append("||||||F\r");
        return sb.ToString();
    }

    private sealed class MeasureReportDto
    {
        public string? Id { get; set; }
        public string? Measure { get; set; }
        public PeriodDto? Period { get; set; }
        public List<GroupDto>? Group { get; set; }
    }

    private sealed class PeriodDto
    {
        public string? Start { get; set; }
        public string? End { get; set; }
    }

    private sealed class GroupDto
    {
        public int Count { get; set; }
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var c in value)
        {
            if (Array.IndexOf(Hl7EscapeChars, c) >= 0) sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
