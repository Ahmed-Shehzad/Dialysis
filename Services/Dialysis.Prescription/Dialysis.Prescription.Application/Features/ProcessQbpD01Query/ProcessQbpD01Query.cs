using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.ProcessQbpD01Query;

/// <summary>
/// Processes a QBP^D01 prescription query and returns the RSP^K22 response as HL7.
/// </summary>
public sealed record ProcessQbpD01QueryCommand(string RawHl7Message) : ICommand<ProcessQbpD01QueryResponse>;
