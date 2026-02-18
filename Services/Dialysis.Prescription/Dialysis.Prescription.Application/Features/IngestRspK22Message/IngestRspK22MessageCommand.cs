using Dialysis.Prescription.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.IngestRspK22Message;

public sealed record IngestRspK22MessageCommand(
    string RawHl7Message,
    RspK22ValidationContext? ValidationContext = null,
    PrescriptionConflictPolicy ConflictPolicy = PrescriptionConflictPolicy.Reject) : ICommand<IngestRspK22MessageResponse>;
