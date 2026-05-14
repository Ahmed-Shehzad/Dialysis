using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

public enum ExportJobStatus
{
    Queued,
    InProgress,
    Completed,
    Failed,
    Cancelled,
}

public enum ExportScope
{
    System,
    Patient,
    Group,
}

public sealed record ExportJob(
    string Id,
    ExportScope Scope,
    string? GroupId,
    IReadOnlyList<string> ResourceTypes,
    DateTimeOffset? Since,
    string? DeIdentificationProfile,
    string? RequestorId,
    ExportJobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ExportJobOutput> Outputs,
    string? Error);

public sealed record ExportJobOutput(string ResourceType, string Url, long ResourceCount);
