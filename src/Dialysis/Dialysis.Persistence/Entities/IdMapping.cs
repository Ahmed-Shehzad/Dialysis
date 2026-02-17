namespace Dialysis.Persistence.Entities;

/// <summary>
/// Maps PDMS resource IDs to external system IDs. Phase 4.2.2.
/// </summary>
public sealed class IdMapping
{
    public Ulid Id { get; set; }
    public string TenantId { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string LocalId { get; set; } = "";
    public string ExternalSystem { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    public static IdMapping Create(string tenantId, string resourceType, string localId, string externalSystem, string externalId)
    {
        return new IdMapping
        {
            Id = Ulid.NewUlid(),
            TenantId = tenantId,
            ResourceType = resourceType,
            LocalId = localId,
            ExternalSystem = externalSystem,
            ExternalId = externalId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
