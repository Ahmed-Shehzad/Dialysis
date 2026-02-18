using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.ProcessQbpQ22Query;

/// <summary>
/// Processes a QBP^Q22 patient demographics query and returns the RSP^K22 response as HL7.
/// </summary>
public sealed record ProcessQbpQ22QueryCommand(string RawHl7Message) : ICommand<ProcessQbpQ22QueryResponse>;
