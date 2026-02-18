namespace Dialysis.Prescription.Application.Features.ProcessQbpD01Query;

/// <summary>
/// RSP^K22 HL7 response string.
/// </summary>
/// <param name="RspK22Message">The HL7 RSP^K22 message.</param>
/// <param name="Mrn">MRN that was queried (for audit).</param>
public sealed record ProcessQbpD01QueryResponse(string RspK22Message, string Mrn);
