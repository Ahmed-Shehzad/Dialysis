using Dialysis.PDMS.Reporting.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Language-aware resolution: exact locale → primary subtag → language-neutral default, with
/// only published templates eligible.
/// </summary>
public sealed class ReportTemplateResolverTests
{
    private static ReportTemplate Published(string slug, ReportKind kind, string? language)
    {
        var t = new ReportTemplate(Guid.NewGuid(), slug, kind, slug, language);
        t.AppendVersion("body", "ops", DateTime.UtcNow);
        t.Publish(1);
        return t;
    }

    [Fact]
    public void Constructor_Normalizes_Language_To_Lowercase()
    {
        var t = new ReportTemplate(Guid.NewGuid(), "discharge", ReportKind.DischargeLetter, "Discharge", "EN-US");
        t.LanguageCode.ShouldBe("en-us");
    }

    [Fact]
    public void Constructor_Treats_Blank_Language_As_Null()
    {
        var t = new ReportTemplate(Guid.NewGuid(), "discharge", ReportKind.DischargeLetter, "Discharge", "  ");
        t.LanguageCode.ShouldBeNull();
    }

    [Fact]
    public void Resolves_Exact_Locale_Match()
    {
        var de = Published("discharge", ReportKind.DischargeLetter, "de");
        var en = Published("discharge", ReportKind.DischargeLetter, "en");
        var def = Published("discharge", ReportKind.DischargeLetter, null);

        var hit = ReportTemplateResolver.Resolve([de, en, def], ReportKind.DischargeLetter, "en");

        hit.ShouldBe(en);
    }

    [Fact]
    public void Falls_Back_To_Primary_Subtag_When_Region_Specific_Absent()
    {
        var de = Published("discharge", ReportKind.DischargeLetter, "de");
        var def = Published("discharge", ReportKind.DischargeLetter, null);

        // Patient prefers de-DE; only a primary "de" template exists.
        var hit = ReportTemplateResolver.Resolve([de, def], ReportKind.DischargeLetter, "de-DE");

        hit.ShouldBe(de);
    }

    [Fact]
    public void Falls_Back_To_Language_Neutral_Default_When_No_Locale_Match()
    {
        var de = Published("discharge", ReportKind.DischargeLetter, "de");
        var def = Published("discharge", ReportKind.DischargeLetter, null);

        var hit = ReportTemplateResolver.Resolve([de, def], ReportKind.DischargeLetter, "fr");

        hit.ShouldBe(def);
    }

    [Fact]
    public void Returns_Default_When_Preferred_Language_Is_Null()
    {
        var de = Published("discharge", ReportKind.DischargeLetter, "de");
        var def = Published("discharge", ReportKind.DischargeLetter, null);

        var hit = ReportTemplateResolver.Resolve([de, def], ReportKind.DischargeLetter, null);

        hit.ShouldBe(def);
    }

    [Fact]
    public void Returns_Null_When_No_Published_Template_For_Kind()
    {
        var draftOnly = new ReportTemplate(Guid.NewGuid(), "discharge", ReportKind.DischargeLetter, "Discharge", "de");
        draftOnly.AppendVersion("body", "ops", DateTime.UtcNow); // never published

        var hit = ReportTemplateResolver.Resolve([draftOnly], ReportKind.DischargeLetter, "de");

        hit.ShouldBeNull();
    }

    [Fact]
    public void Ignores_Templates_Of_Other_Kinds()
    {
        var shift = Published("shift", ReportKind.ShiftReport, "de");

        var hit = ReportTemplateResolver.Resolve([shift], ReportKind.DischargeLetter, "de");

        hit.ShouldBeNull();
    }
}
