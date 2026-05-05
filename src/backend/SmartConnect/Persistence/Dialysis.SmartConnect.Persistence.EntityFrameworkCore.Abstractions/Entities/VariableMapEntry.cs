namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class VariableMapEntry
{
    public Guid Id { get; set; }

    public int Scope { get; set; }

    public Guid FlowId { get; set; }

    public required string Key { get; set; }

    public required string Value { get; set; }
}
