using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Signatures;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>
/// PDFsharp 6.2-backed <see cref="IPdfSigner"/>. Wires the supplied
/// <see cref="ISigningCertificateResolver"/> chain to
/// <see cref="PdfSharpDefaultSigner"/> (PKCS#7 detached, SHA-256), attaches the resulting
/// <see cref="DigitalSignatureHandler"/> to the document, and lets PDFsharp's Save pipeline
/// compute and embed the signature byte range.
///
/// Macros and AcroForm structure are preserved: we open the document in
/// <see cref="PdfDocumentOpenMode.Modify"/> (incremental update), never touching the
/// catalog's <c>/AA</c>, <c>/OpenAction</c>, or per-field <c>/JS</c> dictionaries.
/// </summary>
public sealed class PdfSharpPdfSigner : IPdfSigner
{
    private readonly IReadOnlyDictionary<PdfSigningCertificateSource, ISigningCertificateResolver> _resolvers;

    public PdfSharpPdfSigner(IEnumerable<ISigningCertificateResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        _resolvers = resolvers.ToDictionary(r => r.Source);
    }

    public async Task<byte[]> SignAsync(ReadOnlyMemory<byte> pdf, PdfSigningRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_resolvers.TryGetValue(request.CertificateSource, out var resolver))
        {
            throw new InvalidOperationException(
                $"No signing-certificate resolver is registered for source '{request.CertificateSource}'.");
        }

        var certificate = await resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);

        using var input = new MemoryStream(pdf.ToArray(), writable: false);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        var signer = new PdfSharpDefaultSigner(certificate, PdfMessageDigestType.SHA256, timeStampAuthorityUri: null!);
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
#pragma warning disable VSTHRD103 // PdfSharp 6 only exposes synchronous Save(Stream); target is in-memory so it is CPU-bound, not I/O.
        document.Save(output);
#pragma warning restore VSTHRD103
        return output.ToArray();
    }
}
