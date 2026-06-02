using Dialysis.PDMS.Reporting.Templating;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Binding tests for the Mustache+Markdown binder. The discharge-letter generator depends on
/// these paths — if the binder mis-renders a placeholder the operator never sees the patient
/// name appear in the letter.
/// </summary>
public sealed class MustacheMarkdownBinderTests
{
    [Fact]
    public void Simple_Placeholder_Is_Substituted()
    {
        var binder = new MustacheMarkdownBinder();

        var rendered = binder.BindMarkdown("Hello {{name}}.", new Dictionary<string, object?> { ["name"] = "Ada" });

        rendered.ShouldBe("Hello Ada.");
    }

    [Fact]
    public void Nested_Property_Path_Resolves()
    {
        var binder = new MustacheMarkdownBinder();
        var values = new Dictionary<string, object?>
        {
            ["patient"] = new { name = "Ada Lovelace", mrn = "MRN-1" },
        };

        var rendered = binder.BindMarkdown("{{patient.name}} ({{patient.mrn}})", values);

        rendered.ShouldBe("Ada Lovelace (MRN-1)");
    }

    [Fact]
    public void Markdown_Strips_To_Plain_Text()
    {
        var binder = new MustacheMarkdownBinder();

        var rendered = binder.BindToPlainText(
            "## {{title}}\n\nThis is **bold** and *italic*.",
            new Dictionary<string, object?> { ["title"] = "Heading" });

        rendered.ShouldContain("Heading");
        rendered.ShouldContain("bold");
        rendered.ShouldNotContain("**");
        rendered.ShouldNotContain("##");
    }

    [Fact]
    public void Missing_Binding_Renders_Empty_String()
    {
        var binder = new MustacheMarkdownBinder();

        var rendered = binder.BindMarkdown("Hello {{name}}!", new Dictionary<string, object?>());

        rendered.ShouldBe("Hello !");
    }
}
