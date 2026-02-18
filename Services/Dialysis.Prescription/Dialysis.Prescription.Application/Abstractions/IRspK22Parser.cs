namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Parses HL7 RSP^K22 (prescription response) messages.
/// </summary>
public interface IRspK22Parser
{
    RspK22ParseResult Parse(string hl7Message);
}
