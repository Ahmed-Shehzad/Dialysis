namespace Dialysis.PublicHealth.Models;

/// <summary>Reportable condition for public health / registry submission.</summary>
public sealed record ReportableCondition
{
    public required string Id { get; init; }
    public required string Code { get; init; }
    public required string Display { get; init; }
    public required string Category { get; init; }
    public string? Jurisdiction { get; init; }
    public bool IsActive { get; init; } = true;
}
