namespace BuildingBlocks;

public abstract class BaseEntity
{
    public Ulid Id { get; private set; } = Ulid.NewUlid();
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; private set; } = null;
    public bool IsDeleted { get; private set; } = false;

    public void ApplyCreatedDateTime() => CreatedAtUtc = DateTime.UtcNow;
    public void ApplyUpdateDateTime() => UpdatedAtUtc = DateTime.UtcNow;

    public void ApplySoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        IsDeleted = true;
    }
}
