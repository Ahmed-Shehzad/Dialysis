using System.ComponentModel.DataAnnotations;

namespace FhirCore.Subscriptions.Data;

public sealed class SubscriptionEntity
{
    [Key]
    [MaxLength(64)]
    public required string Id { get; set; }

    [Required]
    [MaxLength(512)]
    public required string Criteria { get; set; }

    [Required]
    [MaxLength(2048)]
    public required string Endpoint { get; set; }

    [MaxLength(64)]
    public string? EndpointType { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
