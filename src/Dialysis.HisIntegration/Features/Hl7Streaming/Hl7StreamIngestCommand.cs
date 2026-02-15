using Intercessor.Abstractions;

namespace Dialysis.HisIntegration.Features.Hl7Streaming;

public sealed record Hl7StreamIngestCommand : ICommand<Hl7StreamIngestResult>
{
    public required string RawMessage { get; init; }
    public string? MessageType { get; init; }
    public string? TenantId { get; init; }
}

public sealed record Hl7StreamIngestResult
{
    public bool Processed { get; init; }
    public string? PatientId { get; init; }
    public string? EncounterId { get; init; }
    public IReadOnlyList<string> ResourceIds { get; init; } = [];
    public string? Error { get; init; }
}
