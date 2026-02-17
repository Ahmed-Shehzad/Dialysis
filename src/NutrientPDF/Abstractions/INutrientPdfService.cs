using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Abstractions;

/// <summary>
/// Service for PDF and document processing operations using Nutrient .NET SDK.
/// Provides conversion between 100+ file formats per https://www.nutrient.io/guides/dotnet/conversion/
/// </summary>
/// <remarks>
/// This interface aggregates segregated role interfaces (ISP). Clients that need only specific capabilities
/// can depend on the smaller interfaces instead:
/// <list type="bullet">
///   <item><see cref="IPdfDocumentConverter"/> — conversion, merge, OCR</item>
///   <item><see cref="IPdfValidationService"/> — validation</item>
///   <item><see cref="IPdfPageEditor"/> — pages, rotation, watermarks, optimization</item>
///   <item><see cref="IPdfFormsService"/> — forms</item>
///   <item><see cref="IPdfSignaturesService"/> — digital signatures</item>
///   <item><see cref="IPdfLayersService"/> — layers (OCG)</item>
///   <item><see cref="IPdfRedactionService"/> — redaction</item>
///   <item><see cref="IPdfMetadataService"/> — metadata, embedded files, bookmarks</item>
/// </list>
/// </remarks>
public interface INutrientPdfService :
    IPdfDocumentConverter,
    IPdfValidationService,
    IPdfPageEditor,
    IPdfFormsService,
    IPdfSignaturesService,
    IPdfLayersService,
    IPdfRedactionService,
    IPdfMetadataService
{
}

/// <summary>
/// Describes a page label range (e.g. pages 1-5 as "i, ii, iii" or "1, 2, 3").
/// </summary>
public sealed record PdfPageLabelRange(int StartPage, PdfPageLabelStyle Style, string Prefix = "", int StartNumber = 1);

/// <summary>
/// Page label style for custom page numbering.
/// </summary>
public enum PdfPageLabelStyle
{
    DecimalArabicNumerals,
    UppercaseRomanNumerals,
    LowercaseRomanNumerals,
    UppercaseLetters,
    LowercaseLetters
}

/// <summary>
/// Position for a digital signature when not using an existing signature field. Values in points.
/// </summary>
/// <param name="Page">1-based page number where the signature will appear.</param>
/// <param name="Left">X coordinate (left edge).</param>
/// <param name="Top">Y coordinate from top of page (top-left origin).</param>
/// <param name="Width">Signature width.</param>
/// <param name="Height">Signature height.</param>
public sealed record PdfSignaturePosition(int Page, float Left, float Top, float Width, float Height);

/// <summary>
/// Information about an applied digital signature in a PDF.
/// </summary>
public sealed record PdfSignatureInfo(
    string Name,
    string Reason,
    string Location,
    string ContactInfo,
    string Date,
    int Page,
    bool CertificateValid,
    string CertificateFriendlyName,
    string CertificateIssuer,
    string CertificateSubject,
    DateTime CertificateNotBefore,
    DateTime CertificateNotAfter);

/// <summary>
/// Information about a signature form field (placeholder) in a PDF.
/// </summary>
public sealed record PdfSignatureFieldInfo(int Id, string Name, int Page);

/// <summary>
/// Information about a PDF layer (optional content group / OCG).
/// </summary>
public sealed record PdfLayerInfo(
    int Id,
    string Title,
    PdfLayerVisibility ViewState,
    PdfLayerVisibility PrintState,
    PdfLayerVisibility ExportState,
    bool Locked);

/// <summary>
/// Visibility state for a PDF layer.
/// </summary>
public enum PdfLayerVisibility
{
    On,
    Off,
    Undefined
}

/// <summary>
/// Information about a PDF form field.
/// </summary>
/// <param name="Id">Internal form field ID.</param>
/// <param name="Title">Form field title/name.</param>
/// <param name="Type">Field type (e.g. Text, CheckBox, ListBox).</param>
/// <param name="Value">Current field value.</param>
/// <param name="Page">1-based page number where the field is located.</param>
public sealed record PdfFormFieldInfo(int Id, string Title, string Type, string Value, int Page);

/// <summary>
/// An item in a combo box or list box form field.
/// </summary>
/// <param name="Text">Display text of the item.</param>
/// <param name="Value">Export value (may differ from display text).</param>
public sealed record PdfFormFieldItem(string Text, string Value);

/// <summary>
/// Check box style for the checked state.
/// </summary>
public enum PdfCheckBoxStyle
{
    Check = 0,
    Circle = 1,
    Cross = 2,
    Diamond = 3,
    Square = 4,
    Star = 5
}

/// <summary>
/// Result of PDF/A validation with detailed XML report.
/// </summary>
/// <param name="IsValid">True if the document conforms to the specified PDF/A standard.</param>
/// <param name="XmlReport">Machine-readable XML validation report. Summarizes problems when not conformant.</param>
public sealed record PdfAValidationResult(bool IsValid, string XmlReport);

/// <summary>
/// RGB color for PDF elements.
/// </summary>
/// <param name="Red">0-255.</param>
/// <param name="Green">0-255.</param>
/// <param name="Blue">0-255.</param>
public sealed record PdfRgbColor(byte Red, byte Green, byte Blue);

/// <summary>
/// PDF encryption strength. Values match GdPicture PdfEncryption.
/// </summary>
public enum PdfEncryptionLevel
{
    Rc4_40 = 1,
    Rc4_128 = 2,
    Aes128 = 3,
    Aes256 = 4,
    Aes256Ex = 5
}

/// <summary>
/// Standard PDF document metadata.
/// </summary>
/// <param name="Title">Document title.</param>
/// <param name="Author">Author name.</param>
/// <param name="Subject">Subject.</param>
/// <param name="Keywords">Keywords (comma-separated or otherwise).</param>
/// <param name="Creator">Creating application.</param>
public sealed record PdfMetadata(string? Title = null, string? Author = null, string? Subject = null, string? Keywords = null, string? Creator = null, DateTimeOffset? CreationDate = null, DateTimeOffset? ModificationDate = null, string? Producer = null);

/// <summary>
/// Width and height of a PDF page in points (1/72 inch).
/// </summary>
public sealed record PdfPageSize(float Width, float Height);

/// <summary>
/// Options for PDF optimization to reduce file size.
/// </summary>
public sealed class PdfOptimizationOptions
{
    /// <summary>Enable Deflate compression. Default: true.</summary>
    public bool EnableCompression { get; set; } = true;
    /// <summary>Pack document (remove unused objects). Default: true.</summary>
    public bool PackDocument { get; set; } = true;
    /// <summary>Linearize PDF for Fast Web View (progressive display while downloading). Default: false.</summary>
    public bool Linearize { get; set; }
    /// <summary>Use GdPicturePDFReducer for advanced optimization (font subsetting, image compression). Default: false.</summary>
    public bool UseReducer { get; set; }
}

/// <summary>
/// A rectangular region to redact. Coordinates in points (1/72 inch), origin top-left.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="Left">X coordinate of left edge.</param>
/// <param name="Top">Y coordinate of top edge.</param>
/// <param name="Width">Width of the region.</param>
/// <param name="Height">Height of the region.</param>
public sealed record PdfRedactionRegion(int Page, float Left, float Top, float Width, float Height);

/// <summary>
/// Information about an embedded file (attachment) in a PDF.
/// </summary>
/// <param name="Index">0-based index for extraction.</param>
/// <param name="Name">File name.</param>
/// <param name="Title">Display title.</param>
/// <param name="Size">File size in bytes.</param>
/// <param name="Description">Optional description.</param>
public sealed record PdfEmbeddedFileInfo(int Index, string Name, string Title, long Size, string? Description);

/// <summary>
/// Information about an extracted inline image from a PDF page.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="ImageIndex">0-based index within the page.</param>
/// <param name="ResourceName">Internal resource name.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="ImageData">Raw image bytes (PNG or JPEG format).</param>
public sealed record PdfExtractedImageInfo(int PageNumber, int ImageIndex, string ResourceName, int Width, int Height, byte[] ImageData);

/// <summary>
/// Information about an annotation (sticky note, highlight, stamp, link, etc.) in a PDF.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="Index">0-based annotation index on the page.</param>
/// <param name="Type">Annotation type (e.g. Text, Highlight, Link, Stamp).</param>
/// <param name="Contents">Annotation text/contents.</param>
/// <param name="Author">Annotation author if set.</param>
/// <param name="Subject">Annotation subject if set.</param>
public sealed record PdfAnnotationInfo(int PageNumber, int Index, string Type, string? Contents, string? Author, string? Subject);

/// <summary>
/// A text match found in a PDF search, with position and dimensions in points.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="Left">X coordinate (left edge) in points.</param>
/// <param name="Top">Y coordinate (top edge) in points.</param>
/// <param name="Width">Width in points.</param>
/// <param name="Height">Height in points.</param>
/// <param name="Text">The matched text.</param>
public sealed record PdfTextMatch(int Page, float Left, float Top, float Width, float Height, string Text);

/// <summary>
/// A document source for merge operations. Stream and format hint (e.g. ".pdf", ".docx") are required.
/// Note: Stream is excluded from equality comparison; use FormatHint for record equality.
/// </summary>
public sealed record PdfMergeSource
{
    public Stream Stream { get; }
    public string FormatHint { get; }

    public PdfMergeSource(Stream stream, string formatHint)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        FormatHint = formatHint ?? throw new ArgumentNullException(nameof(formatHint));
    }

    public void Deconstruct(out Stream stream, out string formatHint) => (stream, formatHint) = (Stream, FormatHint);
}

/// <summary>
/// A bookmark (outline entry) in a PDF.
/// </summary>
public sealed record PdfBookmark(int Id, string Title, int Page, int? ParentId, int Level);

/// <summary>
/// PDF/A conformance levels for archiving.
/// </summary>
public enum PdfAConformance
{
    PdfA1a,
    PdfA1b,
    PdfA2a,
    PdfA2u,
    PdfA2b,
    PdfA3a,
    PdfA3u,
    PdfA3b,
    PdfA4,
    PdfA4e,
    PdfA4f
}
