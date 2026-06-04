using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Direct;

public interface IDirectCertificateResolver
{
    /// <summary>Recipient cert lookup. Production: DNS CERT (RFC 4398) then LDAP DirectTrust fallback.</summary>
    ValueTask<X509Certificate2?> ResolveAsync(string emailAddress, CancellationToken cancellationToken);
}

public interface IDirectSmtpRelay
{
    ValueTask SendAsync(string fromAddress, string toAddress, byte[] mimeEnvelope, CancellationToken cancellationToken);
}

/// <summary>
/// Builds an S/MIME-signed-and-encrypted message and hands it to an outbound SMTP relay. The relay
/// itself is module-supplied — the building block does not embed an SMTP server.
/// </summary>
public sealed class SmtpDirectMessenger : IDirectMessenger
{
    private readonly IDirectCertificateResolver _certificateResolver;
    private readonly IDirectSmtpRelay _relay;
    private readonly X509Certificate2 _senderCertificate;
    private readonly ILogger<SmtpDirectMessenger> _logger;
    /// <summary>
    /// Builds an S/MIME-signed-and-encrypted message and hands it to an outbound SMTP relay. The relay
    /// itself is module-supplied — the building block does not embed an SMTP server.
    /// </summary>
    public SmtpDirectMessenger(IDirectCertificateResolver certificateResolver,
        IDirectSmtpRelay relay,
        X509Certificate2 senderCertificate,
        ILogger<SmtpDirectMessenger> logger)
    {
        _certificateResolver = certificateResolver;
        _relay = relay;
        _senderCertificate = senderCertificate;
        _logger = logger;
    }
    public async ValueTask SendAsync(DirectMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var recipientCert = await _certificateResolver.ResolveAsync(message.ToAddress, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No Direct certificate found for {message.ToAddress}.");

        var mime = BuildMime(message);
        var signed = Sign(mime, _senderCertificate);
        var encrypted = Encrypt(signed, recipientCert);

        await _relay.SendAsync(message.FromAddress, message.ToAddress, encrypted, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Direct message dispatched: {Subject} → {To}", message.Subject, message.ToAddress);
    }

    private static byte[] BuildMime(DirectMessage message)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine("Content-Type: multipart/mixed; boundary=\"==dialysis-direct==\"");
        sb.AppendLine();
        sb.AppendLine("--==dialysis-direct==");
        sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
        sb.AppendLine();
        sb.AppendLine(message.TextBody);
        if (message.Attachment is { } attachment)
        {
            sb.AppendLine("--==dialysis-direct==");
            sb.AppendLine($"Content-Type: {attachment.ContentType}; name=\"{attachment.FileName}\"");
            sb.AppendLine("Content-Transfer-Encoding: base64");
            sb.AppendLine();
            sb.AppendLine(Convert.ToBase64String(attachment.Payload));
        }
        sb.AppendLine("--==dialysis-direct==--");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] Sign(byte[] content, X509Certificate2 signer)
    {
        var contentInfo = new ContentInfo(content);
        var signedCms = new SignedCms(contentInfo, detached: false);
        signedCms.ComputeSignature(new CmsSigner(signer));
        return signedCms.Encode();
    }

    private static byte[] Encrypt(byte[] content, X509Certificate2 recipient)
    {
        var envelopedCms = new EnvelopedCms(new ContentInfo(content));
        envelopedCms.Encrypt(new CmsRecipient(recipient));
        return envelopedCms.Encode();
    }
}
