namespace Dialysis.Treatment.Application.Abstractions;

/// <summary>
/// Parses HL7 ORU^R01 (PCD-01) messages.
/// </summary>
public interface IOruMessageParser
{
    OruParseResult Parse(string hl7Message);
}
