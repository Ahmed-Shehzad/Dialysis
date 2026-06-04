using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.GetDocumentPreview;

/// <summary>
/// What rendering hint the SPA should use for an inline preview. PDFs go through pdfjs;
/// XML / CDA goes through the pretty-printer + a syntax-highlighting code panel; plain
/// text is shown verbatim; anything else falls back to a download-only card.
/// </summary>
public enum DocumentPreviewFormat
{
    Pdf,
    Xml,
    Text,
    Binary,
}

/// <summary>
/// Preview envelope for non-PDF documents. PDFs return only the format hint — the SPA still
/// renders them through the existing pdfjs path against <c>/binary</c>. XML / CDA documents
/// return pretty-printed text the SPA renders in a code panel. Office, image, and other
/// binary blobs return the <see cref="DocumentPreviewFormat.Binary"/> hint with no content;
/// the SPA links to <c>/binary</c> for download.
/// </summary>
public sealed record DocumentPreview(
    DocumentPreviewFormat Format,
    string? Content,
    string MimeType,
    string? RootElement,
    string? DocumentTypeName);

public sealed record GetDocumentPreviewQuery(Guid Id) : IQuery<DocumentPreview?>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsView;
}

public sealed class GetDocumentPreviewQueryHandler(
    IDocumentReferenceRepository repository,
    IDocumentBlobStore blobs)
    : IQueryHandler<GetDocumentPreviewQuery, DocumentPreview?>
{
    // CDA documents commonly arrive as one of these. Plain XML uses the same code path.
    private static readonly HashSet<string> _XmlMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/xml",
        "text/xml",
        "application/cda+xml",
        "application/hl7-cda+xml",
        "application/fhir+xml",
    };

    private static readonly HashSet<string> _TextMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "text/markdown",
    };

    public async Task<DocumentPreview?> HandleAsync(GetDocumentPreviewQuery request, CancellationToken cancellationToken)
    {
        var document = await repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (document is null) return null;

        var mime = document.MimeType;
        if (string.Equals(mime, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return new DocumentPreview(DocumentPreviewFormat.Pdf, Content: null, MimeType: mime, RootElement: null, DocumentTypeName: null);

        var bytes = await blobs.ReadAsync(document.StorageRef, cancellationToken).ConfigureAwait(false);
        if (bytes is null) return null;

        if (_XmlMimeTypes.Contains(mime))
            return BuildXmlPreview(bytes, mime);

        if (_TextMimeTypes.Contains(mime))
            return new DocumentPreview(
                DocumentPreviewFormat.Text,
                Encoding.UTF8.GetString(bytes),
                mime,
                RootElement: null,
                DocumentTypeName: null);

        return new DocumentPreview(DocumentPreviewFormat.Binary, Content: null, MimeType: mime, RootElement: null, DocumentTypeName: null);
    }

    private static DocumentPreview BuildXmlPreview(byte[] bytes, string mime)
    {
        // Disable DTD processing — CDA documents in the wild reference public IDs that we
        // don't want to fetch (privacy + DOS surface). Pretty-printing only needs the doc
        // tree, not external entities.
        var readerSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
        };
        string? rootName = null;
        try
        {
            using var input = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(input, readerSettings);
            var xdoc = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            rootName = xdoc.Root?.Name.LocalName;

            var output = new StringBuilder();
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = false,
                OmitXmlDeclaration = false,
            };
            using (var writer = XmlWriter.Create(output, writerSettings))
            {
                xdoc.Save(writer);
            }

            // Heuristic: a CDA ClinicalDocument root is the de-facto signal. Tagging it lets
            // the SPA pick a CDA-flavored card / icon rather than generic XML.
            var docType = string.Equals(rootName, "ClinicalDocument", StringComparison.Ordinal)
                ? "HL7 CDA"
                : rootName is "Bundle" or "Patient" or "Composition" ? "FHIR (XML)" : "XML";

            return new DocumentPreview(DocumentPreviewFormat.Xml, output.ToString(), mime, rootName, docType);
        }
        catch (XmlException)
        {
            // Malformed XML — fall back to text so the operator still sees something.
            return new DocumentPreview(
                DocumentPreviewFormat.Text,
                Encoding.UTF8.GetString(bytes),
                mime,
                RootElement: rootName,
                DocumentTypeName: "Malformed XML");
        }
    }
}
