namespace Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;

public sealed class SubscriptionRecord
{
    public required string Id { get; set; }

    public required string TopicUrl { get; set; }

    public required SubscriptionChannelType ChannelType { get; set; }

    public required string ChannelEndpoint { get; set; }

    public string? ChannelHeader { get; set; }

    public required string FilterParametersJson { get; set; }

    public required SubscriptionStatus Status { get; set; }

    public int ConsecutiveFailures { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastNotificationAt { get; set; }
}

public sealed class NotificationOutboxRecord
{
    public Guid Id { get; set; }

    public required string SubscriptionId { get; set; }

    public required string PayloadJson { get; set; }

    public required DateTimeOffset EnqueuedAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
