namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Builds QBP^D01^QBP_D01 prescription query messages.
/// Query: MSH, QPD, RCP.
/// QPD-1: MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC
/// QPD-2: Query tag (e.g. YYYYMMDDHHMMSSZZZ)
/// QPD-3: @PID.3^{MRN}^^^^MR
/// </summary>
public static class QbpD01Builder
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    /// <summary>
    /// Build a QBP^D01 query message for prescription by MRN.
    /// </summary>
    /// <param name="mrn">Patient Medical Record Number.</param>
    /// <param name="sendingApp">MSH-3 Sending Application.</param>
    /// <param name="queryTag">Unique query tag (default: timestamp).</param>
    public static string Build(string mrn, string? sendingApp = null, string? queryTag = null)
    {
        queryTag ??= DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmsszzz", System.Globalization.CultureInfo.InvariantCulture);
        string msh3 = string.IsNullOrEmpty(sendingApp) ? "EMR^EMR" : sendingApp;

        string msh = $"MSH{FieldSeparator}^~\\&{FieldSeparator}{msh3}{FieldSeparator}Facility{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}QBP{ComponentSeparator}D01{ComponentSeparator}QBP_D01{FieldSeparator}{queryTag}{FieldSeparator}P{FieldSeparator}2.6";
        string qpd = $"QPD{FieldSeparator}MDC_HDIALY_RX_QUERY{ComponentSeparator}Hemodialysis Prescription Query{ComponentSeparator}MDC{FieldSeparator}{queryTag}{FieldSeparator}@PID.3{ComponentSeparator}{mrn}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}MR";
        string rcp = $"RCP{FieldSeparator}I{FieldSeparator}{FieldSeparator}RD";

        return $"{msh}\r\n{qpd}\r\n{rcp}\r\n";
    }
}
