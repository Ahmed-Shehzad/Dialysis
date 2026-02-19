namespace Dialysis.Fhir.Infrastructure.Persistence;

/// <summary>
/// Entity for persisting FHIR Subscription resources.
/// </summary>
public sealed class SubscriptionEntity
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ChannelType { get; set; }
    public string? Endpoint { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public string ResourceJson { get; set; } = string.Empty;
}
