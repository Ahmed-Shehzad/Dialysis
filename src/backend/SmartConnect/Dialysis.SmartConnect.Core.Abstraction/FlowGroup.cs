namespace Dialysis.SmartConnect;

/// <summary>
/// Logical grouping for flows (analogous to Mirth Connect channel groups).
/// </summary>
public sealed class FlowGroup
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }
}
