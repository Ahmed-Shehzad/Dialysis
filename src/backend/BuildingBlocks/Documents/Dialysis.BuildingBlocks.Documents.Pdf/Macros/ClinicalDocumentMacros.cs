using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Macros;

/// <summary>
/// Reusable QuestPDF container "macros" — extension methods on <see cref="IContainer"/> that
/// encapsulate the clinical-letter house style. Every generator composes its layout from
/// these primitives instead of hand-rolling colours, fonts, and spacings, so the visual
/// language stays consistent across discharge letters, shift reports, billing summaries,
/// and any future report kind.
///
/// Macros never own data — they only style the container they're applied to. Compose with
/// QuestPDF's fluent <c>.Element(c =&gt; c.MacroName())</c> pattern.
/// </summary>
public static class ClinicalDocumentMacros
{
    /// <summary>
    /// The platform's accent colour — used on section underlines, alert borders, and the
    /// header band. Kept in one place so a rebrand only edits this constant.
    /// </summary>
    public static string AccentColor => Colors.Blue.Darken2;

    /// <summary>Muted text colour for sub-titles and metadata.</summary>
    public static string MutedTextColor => Colors.Grey.Darken2;

    /// <summary>Background tint for callouts (alerts, warnings, key-facts boxes).</summary>
    public static string CalloutTint => Colors.Blue.Lighten5;

    /// <summary>
    /// The standard report header band — light tint, accent underline, used at the top of
    /// every report kind. Compose with a child column that places the title + subtitle.
    /// </summary>
    public static IContainer ClinicalHeader(this IContainer container) =>
        container
            .Background(CalloutTint)
            .Padding(12)
            .BorderBottom(2)
            .BorderColor(AccentColor);

    /// <summary>
    /// Section-title styling — semibold 12pt with a thin accent underline so sections are
    /// scannable without dominating the page.
    /// </summary>
    public static IContainer SectionTitleStyle(this IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(AccentColor)
            .PaddingBottom(2)
            .PaddingTop(6);

    /// <summary>
    /// Callout box — used for clinically significant facts (drug allergies, critical
    /// vitals, pending follow-ups). Renders with a left accent stripe and a tint fill.
    /// </summary>
    public static IContainer CalloutBox(this IContainer container) =>
        container
            .Background(CalloutTint)
            .BorderLeft(3)
            .BorderColor(AccentColor)
            .Padding(8);

    /// <summary>
    /// Alert box — for clinically critical facts that must catch the reviewer's eye
    /// (e.g. drug-allergy warnings, critical alarms). Same shape as callout but with the
    /// platform's danger palette.
    /// </summary>
    public static IContainer AlertBox(this IContainer container) =>
        container
            .Background(Colors.Red.Lighten5)
            .BorderLeft(3)
            .BorderColor(Colors.Red.Darken2)
            .Padding(8);

    /// <summary>
    /// Standard table cell padding — applied to every cell so columns read consistently.
    /// </summary>
    public static IContainer TableCell(this IContainer container) =>
        container.Padding(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

    /// <summary>
    /// Section heading rendered as a styled text block. Use when you want the title and
    /// content composed in a single column item.
    /// </summary>
    public static void SectionHeading(this IContainer container, string text) =>
        container.SectionTitleStyle().Text(text).SemiBold().FontSize(12);

    /// <summary>
    /// Renders a key/value pair row — left column for the label, right column for the
    /// value. Width tuned to the German clinical-letter convention.
    /// </summary>
    public static void KeyValueRow(this IContainer container, string key, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(140).Text(key).SemiBold();
            row.RelativeItem().Text(value);
        });
    }

    /// <summary>
    /// Standard page footer — page number / total, muted text, centred. Reused across
    /// every report kind via this single macro.
    /// </summary>
    public static void StandardFooter(this IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Medium));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }
}
