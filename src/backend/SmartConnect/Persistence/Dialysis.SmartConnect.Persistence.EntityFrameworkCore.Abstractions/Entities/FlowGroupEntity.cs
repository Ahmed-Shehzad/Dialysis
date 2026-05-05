namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class FlowGroupEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }
}
