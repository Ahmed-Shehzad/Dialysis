namespace Dialysis.HIE.Outbound.Partners.Direct;

/// <summary>
/// Host configuration for Direct secure messaging (<c>Hie:Direct</c>). When a sender certificate and
/// a pickup directory are present the HIE host wires the <c>SmtpDirectMessenger</c> object graph; a
/// partner can then be routed over <c>Direct</c> transport.
/// </summary>
public sealed class DirectMessagingOptions
{
    public const string SectionName = "Hie:Direct";

    /// <summary>Base64-encoded PKCS#12 (PFX) holding the sender's Direct signing certificate + key.</summary>
    public string? SenderCertificateBase64 { get; set; }

    /// <summary>Password for <see cref="SenderCertificateBase64"/>.</summary>
    public string? SenderCertificatePassword { get; set; }

    /// <summary>
    /// SMTP pickup directory the local MTA polls. The relay drops the S/MIME envelope here as an
    /// <c>.eml</c> file — the dependency-free Direct deployment pattern (no embedded SMTP client).
    /// </summary>
    public string? PickupDirectory { get; set; }

    /// <summary>
    /// Recipient Direct certificates by address (PEM). Production resolves these via DNS CERT / LDAP;
    /// for now they're provisioned in config so the S/MIME encrypt step has a key to encrypt to.
    /// </summary>
    public Dictionary<string, string> RecipientCertificates { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when enough is configured to wire the messenger (sender cert + pickup directory).</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SenderCertificateBase64)
        && !string.IsNullOrWhiteSpace(PickupDirectory);
}
