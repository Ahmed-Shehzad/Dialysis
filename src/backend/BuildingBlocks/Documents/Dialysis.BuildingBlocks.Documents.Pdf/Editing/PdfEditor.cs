using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Editing;

/// <summary>
/// In-process PDF editor operations. Merge, split, extract / remove pages — the
/// operations a clinical workflow needs to assemble (or redact) longitudinal patient
/// dossiers without round-tripping through a desktop tool. Built on PDFsharp 6.x — the
/// same dependency that powers the AcroForms post-processor, so deployments get one
/// consistent PDF object-model dependency.
///
/// All operations are byte-in / byte-out — they never touch the file system. Pass byte
/// arrays in, get a byte array back, hand it to whichever blob store the host wires up.
/// </summary>
public sealed class PdfEditor
{
    /// <summary>
    /// Concatenates each input PDF into one document, preserving page order. Used to
    /// assemble multi-document patient dossiers (e.g. one PDF per session report stitched
    /// into a referral packet).
    /// </summary>
    public static byte[] Merge(IReadOnlyList<ReadOnlyMemory<byte>> pdfDocuments)
    {
        ArgumentNullException.ThrowIfNull(pdfDocuments);
        if (pdfDocuments.Count == 0)
            throw new ArgumentException("At least one input PDF is required.", nameof(pdfDocuments));

        using var output = new PdfDocument();
        foreach (var doc in pdfDocuments)
        {
            using var stream = new MemoryStream(doc.ToArray(), writable: false);
            using var input = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            for (var i = 0; i < input.PageCount; i++)
            {
                output.AddPage(input.Pages[i]);
            }
        }
        return SaveToBytes(output);
    }

    /// <summary>
    /// Splits the input PDF into one byte array per page. Lets a tool that ingests a
    /// multi-page lab report fan it into per-page records.
    /// </summary>
    public static IReadOnlyList<byte[]> SplitByPage(ReadOnlyMemory<byte> pdfDocument)
    {
        using var stream = new MemoryStream(pdfDocument.ToArray(), writable: false);
        using var input = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        var pages = new List<byte[]>(input.PageCount);
        for (var i = 0; i < input.PageCount; i++)
        {
            using var single = new PdfDocument();
            single.AddPage(input.Pages[i]);
            pages.Add(SaveToBytes(single));
        }
        return pages;
    }

    /// <summary>
    /// Returns a new PDF containing only the pages at the supplied 1-based indices, in
    /// the supplied order. Used to redact a longitudinal record down to the pages
    /// relevant to a single GDPR Art. 15 data-subject access request.
    /// </summary>
    public static byte[] ExtractPages(ReadOnlyMemory<byte> pdfDocument, IReadOnlyList<int> pageNumbers)
    {
        ArgumentNullException.ThrowIfNull(pageNumbers);
        if (pageNumbers.Count == 0)
            throw new ArgumentException("Page list is empty.", nameof(pageNumbers));

        using var stream = new MemoryStream(pdfDocument.ToArray(), writable: false);
        using var input = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        using var output = new PdfDocument();
        foreach (var pageNumber in pageNumbers)
        {
            var idx = pageNumber - 1;
            if (idx < 0 || idx >= input.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumbers),
                    $"Page {pageNumber} is out of range (document has {input.PageCount} pages).");
            output.AddPage(input.Pages[idx]);
        }
        return SaveToBytes(output);
    }

    /// <summary>
    /// Returns a new PDF with the supplied 1-based page numbers removed. Used to drop
    /// pages a clinician has flagged as incorrect / out of scope before the document is
    /// shared.
    /// </summary>
    public static byte[] RemovePages(ReadOnlyMemory<byte> pdfDocument, IReadOnlyCollection<int> pageNumbers)
    {
        ArgumentNullException.ThrowIfNull(pageNumbers);
        var toRemove = new HashSet<int>(pageNumbers);
        using var stream = new MemoryStream(pdfDocument.ToArray(), writable: false);
        using var input = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        using var output = new PdfDocument();
        for (var i = 0; i < input.PageCount; i++)
        {
            if (toRemove.Contains(i + 1))
                continue;
            output.AddPage(input.Pages[i]);
        }
        if (output.PageCount == 0)
            throw new InvalidOperationException("Cannot produce an empty PDF — every page was removed.");
        return SaveToBytes(output);
    }

    /// <summary>Returns the page count without copying the document.</summary>
    public static int CountPages(ReadOnlyMemory<byte> pdfDocument)
    {
        using var stream = new MemoryStream(pdfDocument.ToArray(), writable: false);
        using var input = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return input.PageCount;
    }

    private static byte[] SaveToBytes(PdfDocument document)
    {
        using var ms = new MemoryStream();
        // PDFsharp Save(Stream) is sync-only on a MemoryStream; CPU-bound, not I/O-bound.
#pragma warning disable VSTHRD103
        document.Save(ms);
#pragma warning restore VSTHRD103
        return ms.ToArray();
    }
}
