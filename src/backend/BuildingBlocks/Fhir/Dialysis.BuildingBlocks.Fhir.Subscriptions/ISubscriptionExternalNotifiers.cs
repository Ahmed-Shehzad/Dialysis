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
public sealed record SubscriptionEmailNotification
{
    /// <summary>
    /// An email to send for a matched subscription. <paramref name="ToAddress"/> is the subscription's
    /// channel endpoint; <paramref name="FhirBundleJson"/> is the full Backport IG notification Bundle.
    /// </summary>
    public SubscriptionEmailNotification(string ToAddress,
        string Subject,
        string FhirBundleJson,
        FhirSubscriptionRegistration Subscription)
    {
        this.ToAddress = ToAddress;
        this.Subject = Subject;
        this.FhirBundleJson = FhirBundleJson;
        this.Subscription = Subscription;
    }
    public string ToAddress { get; init; }
    public string Subject { get; init; }
    public string FhirBundleJson { get; init; }
    public FhirSubscriptionRegistration Subscription { get; init; }
    public void Deconstruct(out string ToAddress, out string Subject, out string FhirBundleJson, out FhirSubscriptionRegistration Subscription)
    {
        ToAddress = this.ToAddress;
        Subject = this.Subject;
        FhirBundleJson = this.FhirBundleJson;
        Subscription = this.Subscription;
    }
}

/// <summary>
/// An SMS to send for a matched subscription. SMS cannot carry a full Bundle, so
/// <paramref name="Message"/> is a concise alert; the subscriber fetches detail out-of-band.
/// </summary>
public sealed record SubscriptionSmsNotification
{
    /// <summary>
    /// An SMS to send for a matched subscription. SMS cannot carry a full Bundle, so
    /// <paramref name="Message"/> is a concise alert; the subscriber fetches detail out-of-band.
    /// </summary>
    public SubscriptionSmsNotification(string ToNumber,
        string Message,
        FhirSubscriptionRegistration Subscription)
    {
        this.ToNumber = ToNumber;
        this.Message = Message;
        this.Subscription = Subscription;
    }
    public string ToNumber { get; init; }
    public string Message { get; init; }
    public FhirSubscriptionRegistration Subscription { get; init; }
    public void Deconstruct(out string ToNumber, out string Message, out FhirSubscriptionRegistration Subscription)
    {
        ToNumber = this.ToNumber;
        Message = this.Message;
        Subscription = this.Subscription;
    }
}
