using Dialysis.Prescription.Application.Abstractions;

namespace Dialysis.Prescription.Api.Contracts;

public sealed record IngestRspK22MessageRequest(string RawHl7Message, RspK22ValidationContext? ValidationContext = null);
