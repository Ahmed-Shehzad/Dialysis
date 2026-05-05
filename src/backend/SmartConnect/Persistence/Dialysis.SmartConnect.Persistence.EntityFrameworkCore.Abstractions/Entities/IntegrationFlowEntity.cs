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
}
