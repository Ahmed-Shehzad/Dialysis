using Dialysis.Prescription.Application.Features.IngestRspK22Message;

namespace Dialysis.Prescription.Application.Options;

/// <summary>
/// Configuration for RSP^K22 prescription ingestion.
/// </summary>
public class PrescriptionIngestionOptions
{
    public const string SectionName = "PrescriptionIngestion";

    /// <summary>How to handle conflicts when a prescription for the same OrderId exists. Default: Reject.</summary>
    public PrescriptionConflictPolicy ConflictPolicy { get; set; } = PrescriptionConflictPolicy.Reject;
}
