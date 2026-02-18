namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Parses QBP^Q22^QBP_Q21 patient demographics query messages (IHE ITI-21).
/// Extracts MRN or name search parameters, MSH-10, QPD-2.
/// </summary>
public interface IQbpQ22Parser
{
    QbpQ22ParseResult Parse(string hl7Message);
}
