using System.Globalization;

using Dialysis.Treatment.Application.Abstractions;

namespace Dialysis.Treatment.Infrastructure.Hl7;

/// <summary>
/// Builds HL7 ACK^R01 acknowledgment messages per IHE PCD TF PCD-01 transaction.
/// Structure: MSH, MSA, [ERR].
/// </summary>
public sealed class AckR01Builder : IAckR01Builder
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    public string BuildAccept(string messageControlId)
    {
        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa("AA", messageControlId)
        };

        return string.Join("\r\n", segments) + "\r\n";
    }

    public string BuildError(string messageControlId, string errorText)
    {
        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa("AE", messageControlId),
            BuildErr(errorText)
        };

        return string.Join("\r\n", segments) + "\r\n";
    }

    private static string BuildMsh(string messageControlId)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"MSH{FieldSeparator}^~\\&{FieldSeparator}PDMS{FieldSeparator}FAC{FieldSeparator}MACH{FieldSeparator}FAC{FieldSeparator}{timestamp}{FieldSeparator}{FieldSeparator}ACK{ComponentSeparator}R01{ComponentSeparator}ACK{FieldSeparator}{messageControlId}{FieldSeparator}P{FieldSeparator}2.6";
    }

    private static string BuildMsa(string ackCode, string messageControlId) =>
        $"MSA{FieldSeparator}{ackCode}{FieldSeparator}{messageControlId}";

    private static string BuildErr(string errorText) =>
        $"ERR{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{errorText}";
}
