namespace Dialysis.HIS.Scheduling.Domain;

/// <summary>
/// Bookable calendar resource (room, staff slot, equipment). Used for multi-kind scheduling (RA planning &amp; scheduling).
/// </summary>
public sealed class SchedulingResource
{
    public Guid Id { get; set; }

    /// <summary>Stable code grouping resources for calendars (e.g. <c>room</c>, <c>staff</c>, <c>equipment</c>).</summary>
    public string KindCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsBookable { get; set; }
}
