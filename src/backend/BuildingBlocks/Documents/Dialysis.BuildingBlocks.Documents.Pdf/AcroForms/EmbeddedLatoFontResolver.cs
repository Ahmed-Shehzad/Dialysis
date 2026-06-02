using PdfSharp.Fonts;

namespace Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;

/// <summary>
/// PDFsharp <see cref="IFontResolver"/> backed by an assembly-embedded Lato Regular TTF.
/// We bundle Lato (Open Font Licence, same family QuestPDF ships) so AcroForm fields can
/// always build their default appearance without depending on system fonts. Production
/// hosts may still register their own resolver via
/// <see cref="GlobalFontSettings.FontResolver"/> if they need a specific font family.
///
/// The resolver maps every requested family to the embedded face — the default appearance
/// is only used while a clinician hasn't typed into the field yet, so substituting Lato
/// for any other requested family is acceptable. PDF viewers will fall back to base-14
/// fonts when the user actually fills the field.
/// </summary>
public sealed class EmbeddedLatoFontResolver : IFontResolver
{
    private const string ResourceName = "Dialysis.BuildingBlocks.Documents.Pdf.Fonts.Lato-Regular.ttf";
    private static readonly byte[] _bytes = LoadEmbeddedFont();
    public const string FaceName = "Lato";

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(FaceName);

    public byte[] GetFont(string faceName) => _bytes;

    private static byte[] LoadEmbeddedFont()
    {
        var assembly = typeof(EmbeddedLatoFontResolver).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded font resource '{ResourceName}' missing from {assembly.GetName().Name}.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Installs the resolver as the process-wide PDFsharp font resolver. Idempotent — once
    /// set, subsequent calls are no-ops. The AcroForms processor calls this on every
    /// post-processing invocation so callers don't have to wire it up themselves.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new EmbeddedLatoFontResolver();
        }
    }
}
