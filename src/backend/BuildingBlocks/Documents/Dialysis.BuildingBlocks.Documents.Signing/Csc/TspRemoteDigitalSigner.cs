using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PdfSharp.Pdf.Signatures;

namespace Dialysis.BuildingBlocks.Documents.Signing.Csc;

/// <summary>
/// PDFsharp 6 <see cref="IDigitalSigner"/> that delegates the hash-signing step to a
/// remote TSP via <see cref="IRemoteSignatureService"/>. PDFsharp computes the document
/// byte range, calls <see cref="GetSignatureAsync"/> with the bytes to hash, and embeds
/// the returned PKCS#7 in the signature dictionary — the private key never crosses the
/// process boundary.
///
/// Threading: PDFsharp's signing pipeline calls this instance once per Save. The signer
/// is bound to one credential id at construction time; the dispatcher in
/// <see cref="PdfSharpPdfSigner"/> minted a fresh instance per signing request.
/// </summary>
public sealed class TspRemoteDigitalSigner : IDigitalSigner
{
    private readonly IRemoteSignatureService _remote;
    private readonly string _credentialId;
    private readonly X509Certificate2 _certificate;
    private int? _cachedSize;

    public TspRemoteDigitalSigner(IRemoteSignatureService remote, string credentialId, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        ArgumentNullException.ThrowIfNull(certificate);
        _remote = remote;
        _credentialId = credentialId;
        _certificate = certificate;
    }

    public string CertificateName => _certificate.Subject;

    public async Task<int> GetSignatureSizeAsync()
    {
        if (_cachedSize is { } size)
            return size;
        size = await _remote.EstimateSignatureContainerSizeAsync(_credentialId, CancellationToken.None).ConfigureAwait(false);
        _cachedSize = size;
        return size;
    }

    public async Task<byte[]> GetSignatureAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        // PDFsharp hands us the byte range to sign; SHA-256 the bytes once, hand the digest
        // to the TSP. The TSP returns a detached PKCS#7 / CMS signature ready to embed.
        var digest = await ComputeSha256Async(stream, CancellationToken.None).ConfigureAwait(false);
        return await _remote.SignHashAsync(_credentialId, _certificate, digest, CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task<byte[]> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            sha.TransformBlock(buffer, 0, read, null, 0);
        }
        sha.TransformFinalBlock([], 0, 0);
        return sha.Hash!;
    }
}
