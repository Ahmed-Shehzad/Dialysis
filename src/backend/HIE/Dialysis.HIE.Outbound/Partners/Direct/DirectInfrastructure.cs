using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dialysis.BuildingBlocks.Direct;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.Partners.Direct;

/// <summary>
/// Config-backed <see cref="IDirectCertificateResolver"/>: looks up the recipient's Direct
/// certificate from <c>Hie:Direct:RecipientCertificates</c> (address → PEM). Production swaps this
/// for DNS CERT (RFC 4398) + LDAP DirectTrust discovery; the messenger contract is unchanged.
/// </summary>
public sealed class ConfigDirectCertificateResolver : IDirectCertificateResolver
{
    private readonly DirectMessagingOptions _options;
    public ConfigDirectCertificateResolver(IOptions<DirectMessagingOptions> options) => _options = options.Value;

    public ValueTask<X509Certificate2?> ResolveAsync(string emailAddress, CancellationToken cancellationToken)
    {
        if (_options.RecipientCertificates.TryGetValue(emailAddress, out var pem) && !string.IsNullOrWhiteSpace(pem))
            return ValueTask.FromResult<X509Certificate2?>(X509Certificate2.CreateFromPem(pem));
        return ValueTask.FromResult<X509Certificate2?>(null);
    }
}

/// <summary>
/// Drops the finished S/MIME envelope into an SMTP <b>pickup directory</b> as an <c>.eml</c> file —
/// the local MTA relays it onward. Dependency-free (no embedded SMTP client, no obsolete BCL
/// <c>SmtpClient</c>) and a recognised Direct/HISP deployment pattern.
/// </summary>
public sealed class PickupDirectoryDirectSmtpRelay : IDirectSmtpRelay
{
    private readonly DirectMessagingOptions _options;
    public PickupDirectoryDirectSmtpRelay(IOptions<DirectMessagingOptions> options) => _options = options.Value;

    public async ValueTask SendAsync(string fromAddress, string toAddress, byte[] mimeEnvelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mimeEnvelope);
        if (string.IsNullOrWhiteSpace(_options.PickupDirectory))
            throw new InvalidOperationException("Hie:Direct:PickupDirectory is not configured; cannot relay the Direct message.");

        Directory.CreateDirectory(_options.PickupDirectory);
        var headers = new StringBuilder()
            .Append("From: ").Append(fromAddress).Append("\r\n")
            .Append("To: ").Append(toAddress).Append("\r\n")
            .Append("MIME-Version: 1.0\r\n")
            .Append("Content-Type: application/pkcs7-mime; smime-type=enveloped-data; name=\"smime.p7m\"\r\n")
            .Append("Content-Transfer-Encoding: base64\r\n\r\n")
            .Append(Convert.ToBase64String(mimeEnvelope))
            .Append("\r\n");

        var path = Path.Combine(_options.PickupDirectory, $"{Guid.CreateVersion7():N}.eml");
        await File.WriteAllTextAsync(path, headers.ToString(), cancellationToken).ConfigureAwait(false);
    }
}
