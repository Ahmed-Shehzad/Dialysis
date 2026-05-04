namespace Dialysis.DomainDrivenDesign.Primitives;

/// <summary>
/// Cross-cutting audit metadata. Uses <c>protected</c> setters so aggregates and domain factories
/// control changes (rich model); infrastructure can map columns or call the <c>Record*</c> helpers from interceptors co-located with the domain.
/// </summary>
public abstract class Audit
{
    public DateTime CreatedAt { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public string? UpdatedBy { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public DateTime? DeletedAt { get; protected set; }

    public string? DeletedBy { get; protected set; }

    /// <summary>Call when the aggregate is first persisted or materialized as new.</summary>
    protected void RecordCreation(DateTime utcNow, string? actorId = null)
    {
        CreatedAt = utcNow;
        CreatedBy = actorId;
    }

    /// <summary>Call when the aggregate is modified in a meaningful business sense.</summary>
    protected void RecordUpdate(DateTime utcNow, string? actorId = null)
    {
        UpdatedAt = utcNow;
        UpdatedBy = actorId;
    }

    /// <summary>Call when the aggregate is soft-deleted.</summary>
    protected void RecordSoftDelete(DateTime utcNow, string? actorId = null)
    {
        IsDeleted = true;
        DeletedAt = utcNow;
        DeletedBy = actorId;
    }
}
