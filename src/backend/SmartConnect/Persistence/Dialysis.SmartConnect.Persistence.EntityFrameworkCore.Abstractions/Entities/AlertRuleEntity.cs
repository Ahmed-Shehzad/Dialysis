namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class AlertRuleEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public bool Enabled { get; set; }

    public string? Description { get; set; }

    /// <summary>JSON array of Guid; null/empty array = all flows.</summary>
    public string EnabledFlowIdsJson { get; set; } = "[]";

    /// <summary>JSON array of <c>AlertErrorPattern</c>.</summary>
    public string ErrorPatternsJson { get; set; } = "[]";

    /// <summary>JSON array of <c>AlertActionSlot</c>.</summary>
    public string ActionsJson { get; set; } = "[]";

    /// <summary>0 = no throttling; otherwise the duration in whole seconds.</summary>
    public int ThrottleWindowSeconds { get; set; }

    public int Revision { get; set; } = 1;

    public DateTimeOffset LastModifiedUtc { get; set; }
}
