namespace Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;

/// <summary>
/// Post-processor that takes a flat QuestPDF-rendered PDF and overlays interactive
/// AcroForm widgets at the given placements. Returns the AcroForms-enabled PDF bytes.
///
/// The processor is separate from the renderer because (a) QuestPDF emits flat content
/// streams and has no AcroForms primitive, and (b) the AcroForms layer needs the page-level
/// PDF object model, which the renderer abstracts away. Separation also lets the same
/// processor enrich PDFs that did not come from QuestPDF (e.g. signed letters re-imported
/// from a partner system).
/// </summary>
public interface IAcroFormProcessor
{
    /// <summary>Returns the input PDF with interactive form fields added at <paramref name="placements"/>.</summary>
    Task<byte[]> ApplyFormsAsync(
        ReadOnlyMemory<byte> pdfBytes,
        IReadOnlyList<AcroFormPlacement> placements,
        CancellationToken cancellationToken);
}
