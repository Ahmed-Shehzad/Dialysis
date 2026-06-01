namespace Dialysis.BuildingBlocks.ClinicianNotification;

/// <summary>
/// One sender = one channel (SMS, push, email, voice). Senders are registered keyed on the
/// channel code they handle; the dispatcher resolves all senders for a given channel and
/// fans the request out, taking the first successful delivery.
/// </summary>
public interface IClinicianNotificationSender
{
    /// <summary>Stable channel code — e.g. <c>"sms"</c>, <c>"push.apns"</c>, <c>"push.fcm"</c>, <c>"email"</c>.</summary>
    string ChannelCode { get; }

    /// <summary>Sends one notification. Senders must not throw on transient provider errors — return a failure result instead.</summary>
    Task<ClinicianNotificationResult> SendAsync(ClinicianNotificationRequest request, CancellationToken cancellationToken);
}
