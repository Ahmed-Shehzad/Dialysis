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

    /// <summary>
    /// Populates existing AcroForm field values in an already-form-enabled PDF (e.g. a partner
    /// intake form supplied as a blank PDF that the operator fills out in-app). Unknown fields
    /// are reported in the result; coerces checkbox values from common truthy / falsy strings;
    /// signature fields are left untouched (cryptographic signing goes through <c>IPdfSigner</c>).
    /// </summary>
    Task<AcroFormFillResult> FillFormValuesAsync(
        ReadOnlyMemory<byte> pdfBytes,
        IReadOnlyDictionary<string, string> fieldValues,
        CancellationToken cancellationToken);
}

/// <summary>
/// Returned by <see cref="IAcroFormProcessor.FillFormValuesAsync"/>. <see cref="FilledBytes"/>
/// is the new PDF; <see cref="UnknownFields"/> lists keys from the caller's dictionary that
/// don't exist in the PDF; <see cref="FilledFieldNames"/> lists the keys that were actually
/// applied. Callers persist the bytes and surface the unknowns in the UI so the operator
/// knows their input was partially ignored.
/// </summary>
public sealed record AcroFormFillResult(
    byte[] FilledBytes,
    IReadOnlyList<string> FilledFieldNames,
    IReadOnlyList<string> UnknownFields);
