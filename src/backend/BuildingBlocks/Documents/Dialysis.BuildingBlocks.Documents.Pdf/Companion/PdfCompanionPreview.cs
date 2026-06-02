using QuestPDF.Companion;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Companion;

/// <summary>
/// Thin facade over <see cref="CompanionExtensions"/>. Lets developers preview a generated
/// <see cref="DocumentModel"/> live in the QuestPDF Companion desktop app — the macros and
/// components recompose on every change, so the template-authoring workflow gets hot-reload
/// without any host-side restart.
///
/// Install once per workstation:
/// <code>dotnet tool install --global QuestPDF.Companion</code>
///
/// Then call <c>ShowAsync</c> or <see cref="Show"/> from any dev-time entry point
/// (unit test, scratch console, an "Open in companion" button on the template-authoring
/// page). The companion app listens on <see cref="DefaultPort"/> by default; override via
/// the <c>port</c> argument if you're running multiple sessions side-by-side.
///
/// Never call this from a production code path. The preview wire-protocol is unauthenticated
/// and opens a TCP socket on localhost; it's a developer tool, not a network endpoint.
/// </summary>
public static class PdfCompanionPreview
{
    /// <summary>
    /// QuestPDF Companion's default listening port. Override only if a port-clash forces
    /// you to (e.g. multiple solutions previewing concurrently).
    /// </summary>
    public const int DefaultPort = 12500;

    /// <summary>Sync preview — blocks until the companion app disconnects.</summary>
    public static void Show(QuestPdfDocumentRenderer renderer, DocumentModel document, int port = DefaultPort)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(document);
        renderer.Compose(document).ShowInCompanion(port);
    }

    /// <summary>Async preview — recommended for unit-test exploration of layout changes.</summary>
    public static Task ShowAsync(
        QuestPdfDocumentRenderer renderer,
        DocumentModel document,
        int port = DefaultPort,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(document);
        return renderer.Compose(document).ShowInCompanionAsync(port, cancellationToken);
    }

    /// <summary>
    /// Convenience overload — runs an arbitrary <see cref="IDocument"/> composition through
    /// the companion. Useful when you've built a layout by hand without going through
    /// <see cref="DocumentModel"/>.
    /// </summary>
    public static Task ShowAsync(IDocument document, int port = DefaultPort, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.ShowInCompanionAsync(port, cancellationToken);
    }
}
