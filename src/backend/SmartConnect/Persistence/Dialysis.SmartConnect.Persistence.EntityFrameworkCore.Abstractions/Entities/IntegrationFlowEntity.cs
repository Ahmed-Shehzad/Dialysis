namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class IntegrationFlowEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public int RuntimeState { get; set; }

    public required string PipelineJson { get; set; }

    public string? TagsJson { get; set; }

    public Guid? GroupId { get; set; }

    public string? Description { get; set; }

    /// <summary>JSON array of declared payload formats (e.g. <c>["HL7v2","FHIR"]</c>).</summary>
    public string? DataTypesJson { get; set; }

    /// <summary>JSON array of flow ids this channel depends on for Start.</summary>
    public string? DependenciesJson { get; set; }

    /// <summary>JSON array of channel-level <c>ChannelAttachmentReference</c> entries (capped at ~1.5 MiB).</summary>
    public string? AttachmentsJson { get; set; }
}
