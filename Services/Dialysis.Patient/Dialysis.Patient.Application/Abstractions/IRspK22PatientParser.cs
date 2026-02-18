namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Parses HL7 RSP^K22 patient demographics response messages (IHE ITI-21).
/// Extracts MSA, QAK, and 0..N PID segments.
/// </summary>
public interface IRspK22PatientParser
{
    /// <summary>Parses an RSP^K22 message into structured patient demographics.</summary>
    RspK22PatientParseResult Parse(string hl7Message);
}
