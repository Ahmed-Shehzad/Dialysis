namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirReadResult<T>
{
    public FhirReadResult(T? Resource, string? VersionId = null, DateTimeOffset? LastModified = null)
    {
        this.Resource = Resource;
        this.VersionId = VersionId;
        this.LastModified = LastModified;
    }
    public bool Found => Resource is not null;
    public T? Resource { get; init; }
    public string? VersionId { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public void Deconstruct(out T? Resource, out string? VersionId, out DateTimeOffset? LastModified)
    {
        Resource = this.Resource;
        VersionId = this.VersionId;
        LastModified = this.LastModified;
    }
}
