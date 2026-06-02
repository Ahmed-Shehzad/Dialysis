namespace Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

/// <summary>
/// Converts an input PDF into an Office Open XML (.docx) document. Uses the same
/// structure inference as the Markdown converter — inferred headings map to Word
/// heading styles so the downstream Word document is navigable via the document map.
/// </summary>
public interface IPdfToWordConverter
{
    Task<byte[]> ConvertAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken);
}
