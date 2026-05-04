namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class IntegrationFlowEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public int RuntimeState { get; set; }

    public required string PipelineJson { get; set; }
}
