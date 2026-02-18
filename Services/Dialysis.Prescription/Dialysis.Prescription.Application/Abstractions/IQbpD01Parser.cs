namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Parses QBP^D01^QBP_D01 prescription query messages.
/// Extracts MRN, MSH-10, QPD-2, QPD-1 for response building and validation.
/// </summary>
public interface IQbpD01Parser
{
    QbpD01ParseResult Parse(string hl7Message);
}
