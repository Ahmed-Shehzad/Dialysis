using Intercessor.Abstractions;

namespace Dialysis.HisIntegration.Features.AdtSync;

public sealed record AdtIngestCommand : ICommand<AdtIngestResult>
{
    public required string MessageType { get; init; }
    public required string RawMessage { get; init; }
}

public sealed record AdtIngestResult
{
    public bool Processed { get; init; }
    public string? PatientId { get; init; }
    public string? EncounterId { get; init; }
    public string? Message { get; init; }
}
