namespace Dialysis.BuildingBlocks.ClinicianNotification;

/// <summary>
/// One clinician notification — channel-agnostic payload assembled by the on-call dispatcher.
/// Senders translate the payload into the channel-native shape (SMS body, push payload,
/// email MIME, voice TwiML). PHI minimisation rules apply: the body should never carry
/// patient name, MRN, or diagnosis — see <c>docs/compliance/gdpr-controls.md</c> §
/// Notification minimisation.
/// </summary>
public sealed record ClinicianNotificationRequest
{
    /// <summary>
    /// One clinician notification — channel-agnostic payload assembled by the on-call dispatcher.
    /// Senders translate the payload into the channel-native shape (SMS body, push payload,
    /// email MIME, voice TwiML). PHI minimisation rules apply: the body should never carry
    /// patient name, MRN, or diagnosis — see <c>docs/compliance/gdpr-controls.md</c> §
    /// Notification minimisation.
    /// </summary>
    public ClinicianNotificationRequest(string Channel,
        string Address,
        string Subject,
        string Body,
        string? DeepLink,
        NotificationPriority Priority,
        IReadOnlyDictionary<string, string> Metadata)
    {
        this.Channel = Channel;
        this.Address = Address;
        this.Subject = Subject;
        this.Body = Body;
        this.DeepLink = DeepLink;
        this.Priority = Priority;
        this.Metadata = Metadata;
    }
    public string Channel { get; init; }
    public string Address { get; init; }
    public string Subject { get; init; }
    public string Body { get; init; }
    public string? DeepLink { get; init; }
    public NotificationPriority Priority { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
    public void Deconstruct(out string Channel, out string Address, out string Subject, out string Body, out string? DeepLink, out NotificationPriority Priority, out IReadOnlyDictionary<string, string> Metadata)
    {
        Channel = this.Channel;
        Address = this.Address;
        Subject = this.Subject;
        Body = this.Body;
        DeepLink = this.DeepLink;
        Priority = this.Priority;
        Metadata = this.Metadata;
    }
}

public enum NotificationPriority
{
    Normal = 0,
    High = 1,
    Critical = 2,
}

/// <summary>Outcome of one send attempt against one channel.</summary>
public sealed record ClinicianNotificationResult
{
    /// <summary>Outcome of one send attempt against one channel.</summary>
    public ClinicianNotificationResult(bool Delivered,
        string? ProviderMessageId,
        string? FailureReason)
    {
        this.Delivered = Delivered;
        this.ProviderMessageId = ProviderMessageId;
        this.FailureReason = FailureReason;
    }
    public bool Delivered { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? FailureReason { get; init; }
    public void Deconstruct(out bool Delivered, out string? ProviderMessageId, out string? FailureReason)
    {
        Delivered = this.Delivered;
        ProviderMessageId = this.ProviderMessageId;
        FailureReason = this.FailureReason;
    }
}
