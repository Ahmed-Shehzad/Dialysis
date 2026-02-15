using System.Text;

namespace Dialysis.Registry.Services;

/// <summary>Builds HL7 v2 messages for registry submission (ORU^R01).</summary>
public static class Hl7V2Formatter
{
    private static readonly char[] EscapeChars = ['|', '^', '~', '\\', '&'];

    public static string BuildOruMessage(string sendingApp, string sendingFacility, DateTime timestamp, IReadOnlyList<Hl7Segment> segments)
    {
        var sb = new StringBuilder();
        var msgTime = timestamp.ToString("yyyyMMddHHmmss");
        var controlId = Guid.NewGuid().ToString("N")[..20];

        sb.Append("MSH|^~\\&|").Append(Escape(sendingApp)).Append("|").Append(Escape(sendingFacility))
            .Append("|||").Append(msgTime).Append("||ORU^R01^ORU_R01|").Append(controlId)
            .Append("|P|2.5\r");

        foreach (var seg in segments)
            sb.Append(seg.Type).Append("|").Append(seg.ToHl7()).Append("\r");

        return sb.ToString();
    }

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var c in value)
        {
            if (Array.IndexOf(EscapeChars, c) >= 0) sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}

public sealed record Hl7Segment(string Type, IReadOnlyList<string> Fields)
{
    public string ToHl7() => string.Join("|", Fields.Select(Hl7V2Formatter.Escape));
}
