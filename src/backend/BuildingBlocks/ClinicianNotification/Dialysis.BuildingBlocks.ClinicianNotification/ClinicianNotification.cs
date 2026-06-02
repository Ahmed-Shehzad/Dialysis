namespace Dialysis.BuildingBlocks.ClinicianNotification;

/// <summary>
/// One clinician notification — channel-agnostic payload assembled by the on-call dispatcher.
/// Senders translate the payload into the channel-native shape (SMS body, push payload,
/// email MIME, voice TwiML). PHI minimisation rules apply: the body should never carry
/// patient name, MRN, or diagnosis — see <c>docs/compliance/gdpr-controls.md</c> §
/// Notification minimisation.
/// </summary>
public sealed record ClinicianNotificationRequest(
    string Channel,
    string Address,
    string Subject,
    string Body,
    string? DeepLink,
    NotificationPriority Priority,
    IReadOnlyDictionary<string, string> Metadata);

public enum NotificationPriority
{
    Normal = 0,
    High = 1,
    Critical = 2,
}

/// <summary>Outcome of one send attempt against one channel.</summary>
public sealed record ClinicianNotificationResult(
    bool Delivered,
    string? ProviderMessageId,
    string? FailureReason);
