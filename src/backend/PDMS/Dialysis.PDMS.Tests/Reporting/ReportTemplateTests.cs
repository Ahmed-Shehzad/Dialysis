using Dialysis.PDMS.Reporting.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Version-stack + publish/rollback tests for <see cref="ReportTemplate"/>. The operator UI
/// reads <c>GetPublishedBody()</c> on every render, so the publish + version-bump path needs
/// to be airtight.
/// </summary>
public sealed class ReportTemplateTests
{
    private static ReportTemplate Fresh() =>
        new(Guid.NewGuid(), "discharge-letter", ReportKind.DischargeLetter, "Discharge letter");

    [Fact]
    public void First_Append_Becomes_Version_1()
    {
        var template = Fresh();

        var version = template.AppendVersion("Hello {{patient.name}}", "ops-1", new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));

        version.VersionNumber.ShouldBe(1);
        template.Versions.Count.ShouldBe(1);
        template.PublishedVersionNumber.ShouldBeNull();
    }

    [Fact]
    public void Successive_Appends_Bump_Version_Number()
    {
        var template = Fresh();
        template.AppendVersion("v1", "ops", DateTime.UtcNow);
        var v2 = template.AppendVersion("v2", "ops", DateTime.UtcNow);
        var v3 = template.AppendVersion("v3", "ops", DateTime.UtcNow);

        v2.VersionNumber.ShouldBe(2);
        v3.VersionNumber.ShouldBe(3);
    }

    [Fact]
    public void Publish_Selects_The_Active_Body()
    {
        var template = Fresh();
        template.AppendVersion("first wording", "ops", DateTime.UtcNow);
        template.AppendVersion("second wording", "ops", DateTime.UtcNow);

        template.Publish(2);

        template.GetPublishedBody().ShouldBe("second wording");
    }

    [Fact]
    public void Rollback_Reactivates_Earlier_Version()
    {
        var template = Fresh();
        template.AppendVersion("v1", "ops", DateTime.UtcNow);
        template.AppendVersion("v2", "ops", DateTime.UtcNow);
        template.Publish(2);

        template.Publish(1);

        template.GetPublishedBody().ShouldBe("v1");
    }

    [Fact]
    public void Publishing_Unknown_Version_Throws()
    {
        var template = Fresh();
        template.AppendVersion("v1", "ops", DateTime.UtcNow);

        Should.Throw<InvalidOperationException>(() => template.Publish(99));
    }
}
