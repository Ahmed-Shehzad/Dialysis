namespace Dialysis.Patient.Application.Features.IngestRspK22;

/// <summary>
/// Result of ingesting an RSP^K22 message.
/// </summary>
/// <param name="IngestedCount">Number of patients created or updated.</param>
/// <param name="Status">QAK-2 status from the message: OK, NF, AE, AR.</param>
/// <param name="Skipped">True when status is NF/AE/AR and no patients were ingested.</param>
public sealed record IngestRspK22Response(int IngestedCount, string Status, bool Skipped);
