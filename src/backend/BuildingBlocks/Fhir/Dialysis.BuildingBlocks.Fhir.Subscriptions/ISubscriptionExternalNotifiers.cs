namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// Email delivery for the <see cref="SubscriptionChannelType.Email"/> channel. The Subscriptions
/// building block ships this <b>interface only</b> — a host/module supplies the concrete
/// implementation from its own infrastructure (SendGrid, SES, SMTP, …). When no implementation is
/// registered the Email channel is inert.
/// </summary>
public interface IEmailNotifier
{
    ValueTask SendAsync(SubscriptionEmailNotification notification, CancellationToken cancellationToken);
}

/// <summary>
/// SMS delivery for the <see cref="SubscriptionChannelType.Sms"/> channel. Interface only — the
/// host/module supplies the concrete adapter (Twilio, SNS, …). Inert when unimplemented.
/// </summary>
public interface ISmsNotifier
{
    ValueTask SendAsync(SubscriptionSmsNotification notification, CancellationToken cancellationToken);
}

/// <summary>
/// An email to send for a matched subscription. <paramref name="ToAddress"/> is the subscription's
/// channel endpoint; <paramref name="FhirBundleJson"/> is the full Backport IG notification Bundle.
/// </summary>
public sealed record SubscriptionEmailNotification(
    string ToAddress,
    string Subject,
    string FhirBundleJson,
    FhirSubscriptionRegistration Subscription);

/// <summary>
/// An SMS to send for a matched subscription. SMS cannot carry a full Bundle, so
/// <paramref name="Message"/> is a concise alert; the subscriber fetches detail out-of-band.
/// </summary>
public sealed record SubscriptionSmsNotification(
    string ToNumber,
    string Message,
    FhirSubscriptionRegistration Subscription);
