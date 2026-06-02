namespace Dialysis.BuildingBlocks.Documents.Pdf;

/// <summary>
/// Renders a logical document model into a PDF byte array. Consumers (PDMS Reporting,
/// EHR Billing) build a <see cref="DocumentModel"/> from their aggregate state and let the
/// renderer produce a deterministic PDF — deterministic so the audit gate can hash the bytes
/// and the same input always produces the same output (no embedded build timestamps, no
/// system fonts whose rendering differs across hosts).
/// </summary>
public interface IPdfDocumentRenderer
{
    Task<byte[]> RenderAsync(DocumentModel document, CancellationToken cancellationToken);
}

/// <summary>
/// Output-format-agnostic document model — the renderer translates this into PDF widgets.
/// Reports compose from <see cref="DocumentSection"/> blocks; sections nest their content as
/// paragraphs / tables / key-value pairs. The model intentionally has no styling beyond
/// section/heading levels so the QuestPDF renderer can apply a consistent house style.
/// </summary>
public sealed record DocumentModel(
    string Title,
    string? Subtitle,
    IReadOnlyList<DocumentSection> Sections,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record DocumentSection(
    string Heading,
    IReadOnlyList<DocumentBlock> Blocks);

public abstract record DocumentBlock;

public sealed record ParagraphBlock(string Text) : DocumentBlock;

public sealed record KeyValueBlock(IReadOnlyList<KeyValuePair<string, string>> Pairs) : DocumentBlock;

public sealed record TableBlock(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows) : DocumentBlock;
