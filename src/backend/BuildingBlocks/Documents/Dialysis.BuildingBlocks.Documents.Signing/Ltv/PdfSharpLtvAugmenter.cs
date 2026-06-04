using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Dialysis.BuildingBlocks.Documents.Signing.Ltv;

/// <summary>
/// Post-processor that re-opens a freshly-signed PDF and attaches a Document Security
/// Store (<c>/DSS</c>) dictionary containing the signer cert chain plus any CRL / OCSP
/// blobs the <see cref="RevocationEvidenceCollector"/> gathered. Lifts a PAdES-B-T
/// signature to PAdES-B-LT without re-signing.
///
/// PDFsharp 6.2 has no public DSS API, so we build the dictionary ourselves through the
/// PdfDictionary / PdfArray / indirect-object primitives — the same surface
/// <c>PdfSharpAcroFormProcessor</c> uses to attach AcroForm widgets. Macros and the
/// original signature are never touched.
/// </summary>
public sealed class PdfSharpLtvAugmenter
{
    private readonly ILogger<PdfSharpLtvAugmenter> _logger;

    public PdfSharpLtvAugmenter(ILogger<PdfSharpLtvAugmenter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Returns <paramref name="pdfBytes"/> with a DSS dictionary attached. If
    /// <paramref name="evidence"/> is empty the bytes are returned unchanged.
    /// </summary>
    public Task<byte[]> AugmentAsync(ReadOnlyMemory<byte> pdfBytes, CollectedRevocationEvidence evidence, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ChainCertificates.Count == 0 && evidence.Crls.Count == 0 && evidence.Ocsps.Count == 0)
        {
            return Task.FromResult(pdfBytes.ToArray());
        }

        using var input = new MemoryStream(pdfBytes.ToArray(), writable: false);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        cancellationToken.ThrowIfCancellationRequested();

        var dss = BuildDssDictionary(document, evidence);
        document.Internals.AddObject(dss);
        document.Internals.Catalog.Elements["/DSS"] = dss.Reference;

        // Mark the PDF as a PAdES-B-LT-capable file by bumping the catalog's /Version key
        // — PAdES requires the version to be at least 1.7.
        document.Internals.Catalog.Elements["/Version"] = new PdfName("/1.7");

        using var output = new MemoryStream();
#pragma warning disable VSTHRD103 // Save(Stream) is synchronous; target is in-memory, CPU-bound only.
        document.Save(output);
#pragma warning restore VSTHRD103

        _logger.LogDebug(
            "DSS dictionary attached with {ChainCount} certs, {CrlCount} CRLs, {OcspCount} OCSP responses.",
            evidence.ChainCertificates.Count, evidence.Crls.Count, evidence.Ocsps.Count);

        return Task.FromResult(output.ToArray());
    }

    private static PdfDictionary BuildDssDictionary(PdfDocument owner, CollectedRevocationEvidence evidence)
    {
        var dss = new PdfDictionary(owner);
        dss.Elements.SetName("/Type", "/DSS");

        if (evidence.ChainCertificates.Count > 0)
        {
            dss.Elements["/Certs"] = BuildStreamArray(owner, [.. evidence.ChainCertificates.Select(c => c.RawData)]);
        }
        if (evidence.Crls.Count > 0)
        {
            dss.Elements["/CRLs"] = BuildStreamArray(owner, evidence.Crls);
        }
        if (evidence.Ocsps.Count > 0)
        {
            dss.Elements["/OCSPs"] = BuildStreamArray(owner, evidence.Ocsps);
        }
        return dss;
    }

    private static PdfArray BuildStreamArray(PdfDocument owner, IReadOnlyList<byte[]> blobs)
    {
        var array = new PdfArray(owner);
        foreach (var blob in blobs)
        {
            var stream = new PdfDictionary(owner);
            stream.CreateStream(blob);
            owner.Internals.AddObject(stream);
            array.Elements.Add(stream.Reference!);
        }
        return array;
    }
}
