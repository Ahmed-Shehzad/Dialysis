using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Macros;

/// <summary>
/// Reusable QuestPDF container "macros" — extension methods on <see cref="IContainer"/> that
/// encapsulate the platform's corporate document house style. Every generator composes its layout
/// from these primitives instead of hand-rolling colours, fonts, and spacings, so the visual
/// language stays consistent across discharge letters, shift reports, billing summaries, invoices,
/// and any future document kind.
///
/// The palette mirrors the web apps' Tailwind theme (clinic-green accent + slate neutrals) so a
/// generated PDF reads as the same product as the SPA that produced it. A rebrand only edits the
/// colour tokens + the <see cref="BrandLetterhead"/> mark below; nothing else changes.
///
/// Macros never own data — they only style the container they're applied to. Compose with
/// QuestPDF's fluent <c>.Element(c =&gt; c.MacroName())</c> pattern.
/// </summary>
public static class ClinicalDocumentMacros
{
    // ── Brand palette ───────────────────────────────────────────────────────────────────────────
    // Hex tokens mirroring src/frontend/*/tailwind.config.js (clinic-green + slate). QuestPDF's
    // fluent colour setters accept "#rrggbb" strings directly.

    /// <summary>Primary brand accent — clinic green (web <c>clinic-500</c>). Section rules, stripes.</summary>
    public static string AccentColor => "#00a97a";

    /// <summary>Deep brand green (web <c>clinic-800</c>) — the letterhead band fill.</summary>
    public static string BrandBandColor => "#015941";

    /// <summary>Muted body text — slate-500.</summary>
    public static string MutedTextColor => "#64748b";

    /// <summary>Strong heading / value text — slate-900.</summary>
    public static string HeadingTextColor => "#0f172a";

    /// <summary>Light neutral surface for utility header bands — slate-100.</summary>
    public static string SurfaceTint => "#f1f5f9";

    /// <summary>Callout (info) background tint — emerald-50.</summary>
    public static string CalloutTint => "#ecfdf5";

    /// <summary>Alert background tint — red-50.</summary>
    public static string AlertTint => "#fef2f2";

    /// <summary>Alert stripe / border — red-600 (clinical-safety danger state, kept distinct from brand).</summary>
    public static string AlertColor => "#dc2626";

    /// <summary>Hairline divider for table rows — slate-200.</summary>
    public static string DividerColor => "#e2e8f0";

    /// <summary>Footer / fine-print text — slate-400.</summary>
    public static string FaintTextColor => "#94a3b8";

    /// <summary>On-band primary (white) text for the green letterhead.</summary>
    public static string OnBandColor => "#ffffff";

    /// <summary>On-band secondary text (light mint, web <c>emerald-200</c>) for letterhead sub-lines.</summary>
    public static string OnBandMutedColor => "#a7f3d0";

    /// <summary>
    /// The brand mark, as a path-only SVG (no <c>&lt;text&gt;</c>, so the vector output stays
    /// deterministic for the audit-hash pipeline — no dependency on a system font being present).
    /// A softly-rounded translucent tile carrying a white droplet with a green centre: legible on
    /// the deep-green letterhead band and unmistakably the Dialysis Platform mark.
    /// </summary>
    private const string BrandMarkSvg =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
          <rect width="48" height="48" rx="12" fill="#ffffff" fill-opacity="0.16"/>
          <path d="M24 9C24 9 34 21 34 29A10 10 0 0 1 14 29C14 21 24 9 24 9Z" fill="#ffffff"/>
          <circle cx="24" cy="30" r="3.4" fill="#00a97a"/>
        </svg>
        """;

    // ── Header / footer ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The corporate letterhead band rendered at the very top of every document: a deep-green band
    /// carrying the brand mark + "DIALYSIS / PLATFORM" wordmark on the left and the document
    /// title + subtitle on the right. This is the page header for the whole document kind.
    /// </summary>
    public static void BrandLetterhead(this IContainer container, string title, string? subtitle)
    {
        container
            .Background(BrandBandColor)
            .PaddingVertical(12)
            .PaddingHorizontal(14)
            .Row(row =>
            {
                row.AutoItem().Row(brand =>
                {
                    brand.Spacing(9);
                    brand.ConstantItem(26).AlignMiddle().Svg(BrandMarkSvg);
                    brand.AutoItem().AlignMiddle().Column(col =>
                    {
                        col.Item().Text("DIALYSIS").FontColor(OnBandColor).SemiBold().FontSize(15);
                        col.Item().Text("PLATFORM").FontColor(OnBandMutedColor).FontSize(7);
                    });
                });

                row.RelativeItem().AlignRight().AlignMiddle().Column(col =>
                {
                    col.Item().AlignRight().Text(title).FontColor(OnBandColor).SemiBold().FontSize(14);
                    if (!string.IsNullOrWhiteSpace(subtitle))
                        col.Item().AlignRight().Text(subtitle).FontColor(OnBandMutedColor).FontSize(9);
                });
            });
    }

    /// <summary>
    /// A light utility header band — slate surface with a green accent underline. Used by
    /// <c>PatientHeaderComponent</c> and any inline sub-header that carries dark text (so it stays
    /// readable, unlike the bold <see cref="BrandLetterhead"/> band which is white-on-green).
    /// </summary>
    public static IContainer ClinicalHeader(this IContainer container) =>
        container
            .Background(SurfaceTint)
            .Padding(12)
            .BorderBottom(2)
            .BorderColor(AccentColor);

    /// <summary>
    /// Standard page footer — a thin slate top-rule with "Dialysis Platform · Confidential" on the
    /// left and the page number on the right. Deliberately a single light line (not a filled band)
    /// so it never collides with form fields placed low on the page (e.g. the invoice review block).
    /// </summary>
    public static void StandardFooter(this IContainer container)
    {
        container.BorderTop(0.75f).BorderColor(DividerColor).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().AlignLeft().Text("Dialysis Platform · Confidential")
                .FontSize(7.5f).FontColor(FaintTextColor);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(t => t.FontSize(7.5f).FontColor(FaintTextColor));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    // ── Sections / callouts / tables ────────────────────────────────────────────────────────────

    /// <summary>
    /// Section-title styling — a thin green accent underline so sections are scannable without
    /// dominating the page.
    /// </summary>
    public static IContainer SectionTitleStyle(this IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(AccentColor)
            .PaddingBottom(3)
            .PaddingTop(8);

    /// <summary>
    /// Section heading rendered as a styled text block (slate-900, semibold, green underline). Use
    /// when you want the title and content composed in a single column item.
    /// </summary>
    public static void SectionHeading(this IContainer container, string text) =>
        container.SectionTitleStyle().Text(text).SemiBold().FontSize(11.5f).FontColor(HeadingTextColor);

    /// <summary>
    /// Callout box — for clinically significant facts (key dates, pending follow-ups). Renders with
    /// a green left accent stripe and a light-mint tint fill.
    /// </summary>
    public static IContainer CalloutBox(this IContainer container) =>
        container
            .Background(CalloutTint)
            .BorderLeft(3)
            .BorderColor(AccentColor)
            .Padding(8);

    /// <summary>
    /// Alert box — for clinically critical facts that must catch the reviewer's eye (drug-allergy
    /// warnings, critical alarms). Same shape as the callout but with the danger palette, kept
    /// distinct from the brand green on purpose.
    /// </summary>
    public static IContainer AlertBox(this IContainer container) =>
        container
            .Background(AlertTint)
            .BorderLeft(3)
            .BorderColor(AlertColor)
            .Padding(8);

    /// <summary>
    /// Standard table cell — comfortable padding and a slate hairline bottom divider so columns
    /// read consistently across every report.
    /// </summary>
    public static IContainer TableCell(this IContainer container) =>
        container.PaddingVertical(4).PaddingHorizontal(3).BorderBottom(0.75f).BorderColor(DividerColor);

    /// <summary>
    /// Renders a key/value pair row — a muted slate label on the left, the dark value on the right.
    /// Width tuned to the clinical-letter convention.
    /// </summary>
    public static void KeyValueRow(this IContainer container, string key, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(140).Text(key).SemiBold().FontColor(MutedTextColor);
            row.RelativeItem().Text(value).FontColor(HeadingTextColor);
        });
    }
}
