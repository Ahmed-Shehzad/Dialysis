using Dialysis.BuildingBlocks.Documents.Signing.Csc;
using Dialysis.BuildingBlocks.Documents.Signing.Ltv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Signatures;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>
/// PDFsharp 6.2-backed <see cref="IPdfSigner"/>. Dispatches to either the local
/// <see cref="PdfSharpDefaultSigner"/> (Platform / User resolvers, private key in
/// process) or a <see cref="TspRemoteDigitalSigner"/> (eIDAS-QES, private key at TSP),
/// then optionally invokes <see cref="PdfSharpLtvAugmenter"/> to attach a DSS dictionary
/// when the request asks for PAdES-B-LT or higher.
///
/// Macros and AcroForm structure are preserved: documents are opened in
/// <see cref="PdfDocumentOpenMode.Modify"/> (incremental update). The catalog's
/// <c>/AA</c>, <c>/OpenAction</c>, and per-field <c>/JS</c> dictionaries are never touched.
/// </summary>
public sealed class PdfSharpPdfSigner : IPdfSigner
{
    private readonly IReadOnlyDictionary<PdfSigningCertificateSource, ISigningCertificateResolver> _resolvers;
    private readonly IRemoteSignatureService? _remoteSignature;
    private readonly PdfSharpLtvAugmenter _ltvAugmenter;
    private readonly RevocationEvidenceCollector _evidenceCollector;
    private readonly TsaOptions _tsaOptions;
    private readonly LtvOptions _ltvOptions;
    private readonly ILogger<PdfSharpPdfSigner> _logger;

    public PdfSharpPdfSigner(
        IEnumerable<ISigningCertificateResolver> resolvers,
        PdfSharpLtvAugmenter ltvAugmenter,
        RevocationEvidenceCollector evidenceCollector,
        IOptions<TsaOptions> tsaOptions,
        IOptions<LtvOptions> ltvOptions,
        ILogger<PdfSharpPdfSigner> logger,
        IRemoteSignatureService? remoteSignature = null)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        ArgumentNullException.ThrowIfNull(ltvAugmenter);
        ArgumentNullException.ThrowIfNull(evidenceCollector);
        ArgumentNullException.ThrowIfNull(tsaOptions);
        ArgumentNullException.ThrowIfNull(ltvOptions);
        ArgumentNullException.ThrowIfNull(logger);
        _resolvers = resolvers.ToDictionary(r => r.Source);
        _remoteSignature = remoteSignature;
        _ltvAugmenter = ltvAugmenter;
        _evidenceCollector = evidenceCollector;
        _tsaOptions = tsaOptions.Value;
        _ltvOptions = ltvOptions.Value;
        _logger = logger;
    }

    public async Task<PdfSigningResult> SignAsync(ReadOnlyMemory<byte> pdf, PdfSigningRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_resolvers.TryGetValue(request.CertificateSource, out var resolver))
        {
            throw new InvalidOperationException(
                $"No signing-certificate resolver is registered for source '{request.CertificateSource}'.");
        }
        if (request.Level >= PadesConformance.T && string.IsNullOrWhiteSpace(_tsaOptions.Uri))
        {
            throw new InvalidOperationException(
                "PAdES-T and higher require Documents:Signing:Tsa:Uri to be configured.");
        }
        if (request.CertificateSource == PdfSigningCertificateSource.RemoteQes && _remoteSignature is null)
        {
            throw new InvalidOperationException(
                "RemoteQes signing requested but no IRemoteSignatureService is registered.");
        }

        var certificate = await resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);

        // 1. Local PKCS#7 sign or remote TSP sign — both go through PdfSharp's signature pipeline.
        var initialSignedBytes = SignWithPdfSharp(pdf, request, certificate);

        // 2. Optional LTV augmentation — only when the request asked for it.
        var (finalBytes, evidence) = request.Level >= PadesConformance.Lt && _ltvOptions.Enabled
            ? await AugmentForLtvAsync(initialSignedBytes, certificate, cancellationToken).ConfigureAwait(false)
            : (initialSignedBytes, null);

        var isQualified = request.CertificateSource == PdfSigningCertificateSource.RemoteQes;
        string? tsaUri = null;
        DateTime? timestampedAt = null;
        if (request.Level >= PadesConformance.T)
        {
            tsaUri = _tsaOptions.Uri;
            timestampedAt = DateTime.UtcNow;
        }

        return new PdfSigningResult(
            SignedPdf: finalBytes,
            CertThumbprint: certificate.Thumbprint,
            Level: request.Level,
            IsQualified: isQualified,
            TsaUri: tsaUri,
            TsaCertThumbprint: null,
            TimestampedAtUtc: timestampedAt,
            Revocation: evidence);
    }

    private byte[] SignWithPdfSharp(ReadOnlyMemory<byte> pdf, PdfSigningRequest request, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        using var input = new MemoryStream(pdf.ToArray(), writable: false);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        IDigitalSigner signer = request.CertificateSource switch
        {
            PdfSigningCertificateSource.RemoteQes => new TspRemoteDigitalSigner(
                _remoteSignature!, request.TspCredentialId!, certificate),
            _ => new PdfSharpDefaultSigner(
                certificate,
                PdfMessageDigestType.SHA256,
                request.Level >= PadesConformance.T && _tsaOptions.Uri is { } uri ? new Uri(uri) : null!),
        };

        var options = request.VisiblePlacement is { } visible
            ? new DigitalSignatureOptions
            {
                Reason = request.Reason ?? string.Empty,
                Location = request.Location ?? string.Empty,
                ContactInfo = request.ContactInfo ?? string.Empty,
                AppName = "Dialysis Platform",
                PageIndex = visible.PageNumber - 1,
                Rectangle = new XRect(visible.X, visible.Y, visible.Width, visible.Height),
            }
            : new DigitalSignatureOptions
            {
                Reason = request.Reason ?? string.Empty,
                Location = request.Location ?? string.Empty,
                ContactInfo = request.ContactInfo ?? string.Empty,
                AppName = "Dialysis Platform",
            };

        _ = DigitalSignatureHandler.ForDocument(document, signer, options);

        using var output = new MemoryStream();
#pragma warning disable VSTHRD103 // PdfSharp 6 only exposes Save(Stream); target is in-memory, CPU-bound only.
        document.Save(output);
#pragma warning restore VSTHRD103
        return output.ToArray();
    }

    private async Task<(byte[] bytes, RevocationEvidence? evidence)> AugmentForLtvAsync(
        byte[] signedBytes,
        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        var collected = _evidenceCollector.Collect(certificate);
        if (collected is null)
        {
            _logger.LogWarning("Skipping LTV augmentation — no revocation evidence collected.");
            return (signedBytes, null);
        }
        var augmented = await _ltvAugmenter.AugmentAsync(signedBytes, collected, cancellationToken).ConfigureAwait(false);
        var format = (collected.Crls.Count, collected.Ocsps.Count) switch
        {
            ( > 0, > 0) => RevocationEvidenceKind.Both,
            ( > 0, 0) => RevocationEvidenceKind.Crl,
            (0, > 0) => RevocationEvidenceKind.Ocsp,
            _ => RevocationEvidenceKind.None,
        };
        return (augmented, new RevocationEvidence(format, []));
    }
}
