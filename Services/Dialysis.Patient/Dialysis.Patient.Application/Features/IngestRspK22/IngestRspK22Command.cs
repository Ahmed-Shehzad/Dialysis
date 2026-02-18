using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.IngestRspK22;

/// <summary>
/// Ingests an RSP^K22 patient demographics response (IHE ITI-21).
/// Parses PID segments and creates or updates patients in the local store.
/// </summary>
public sealed record IngestRspK22Command(string RawHl7Message) : ICommand<IngestRspK22Response>;
