namespace BuildingBlocks;

public static class Extensions
{
    public static Ulid? ToUlid(this Guid? guid) => !guid.HasValue ? null : ToUlid(guid.Value);

    public static Ulid ToUlid(this Guid guid) => guid == Guid.Empty ? Ulid.Empty : new Ulid(guid);
}
