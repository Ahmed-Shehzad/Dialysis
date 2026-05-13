namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class CodeTemplateLibraryEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>JSON array of flow Ids this library is explicitly linked to.</summary>
    public required string LinkedFlowIdsJson { get; set; } = "[]";

    public bool AutoLinkNewFlows { get; set; }

    public int Revision { get; set; } = 1;

    public DateTimeOffset LastModifiedUtc { get; set; }
}
