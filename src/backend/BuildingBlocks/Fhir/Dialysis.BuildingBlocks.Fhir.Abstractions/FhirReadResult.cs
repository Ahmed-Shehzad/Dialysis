namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirReadResult<T>(T? Resource, string? VersionId = null, DateTimeOffset? LastModified = null)
{
    public bool Found => Resource is not null;
}
