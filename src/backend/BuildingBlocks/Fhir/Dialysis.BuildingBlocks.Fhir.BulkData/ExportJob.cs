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

public sealed record ExportJob
{
    public ExportJob(string Id,
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
        string? Error)
    {
        this.Id = Id;
        this.Scope = Scope;
        this.GroupId = GroupId;
        this.ResourceTypes = ResourceTypes;
        this.Since = Since;
        this.DeIdentificationProfile = DeIdentificationProfile;
        this.RequestorId = RequestorId;
        this.Status = Status;
        this.CreatedAt = CreatedAt;
        this.CompletedAt = CompletedAt;
        this.Outputs = Outputs;
        this.Error = Error;
    }
    public string Id { get; init; }
    public ExportScope Scope { get; init; }
    public string? GroupId { get; init; }
    public IReadOnlyList<string> ResourceTypes { get; init; }
    public DateTimeOffset? Since { get; init; }
    public string? DeIdentificationProfile { get; init; }
    public string? RequestorId { get; init; }
    public ExportJobStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public IReadOnlyList<ExportJobOutput> Outputs { get; init; }
    public string? Error { get; init; }
    public void Deconstruct(out string Id, out ExportScope Scope, out string? GroupId, out IReadOnlyList<string> ResourceTypes, out DateTimeOffset? Since, out string? DeIdentificationProfile, out string? RequestorId, out ExportJobStatus Status, out DateTimeOffset CreatedAt, out DateTimeOffset? CompletedAt, out IReadOnlyList<ExportJobOutput> Outputs, out string? Error)
    {
        Id = this.Id;
        Scope = this.Scope;
        GroupId = this.GroupId;
        ResourceTypes = this.ResourceTypes;
        Since = this.Since;
        DeIdentificationProfile = this.DeIdentificationProfile;
        RequestorId = this.RequestorId;
        Status = this.Status;
        CreatedAt = this.CreatedAt;
        CompletedAt = this.CompletedAt;
        Outputs = this.Outputs;
        Error = this.Error;
    }
}

public sealed record ExportJobOutput
{
    public ExportJobOutput(string ResourceType, string Url, long ResourceCount)
    {
        this.ResourceType = ResourceType;
        this.Url = Url;
        this.ResourceCount = ResourceCount;
    }
    public string ResourceType { get; init; }
    public string Url { get; init; }
    public long ResourceCount { get; init; }
    public void Deconstruct(out string ResourceType, out string Url, out long ResourceCount)
    {
        ResourceType = this.ResourceType;
        Url = this.Url;
        ResourceCount = this.ResourceCount;
    }
}
