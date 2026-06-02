using Dialysis.PDMS.Reporting.Contracts;
using Dialysis.PDMS.Reporting.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// State-machine tests for <see cref="SessionReport"/>: pending → generated → delivered,
/// plus the failure + archive transitions. The integration event must fire on the first
/// transition into <c>Generated</c>.
/// </summary>
public sealed class SessionReportTests
{
    private static SessionReport Fresh() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ReportKind.DischargeLetter);

    [Fact]
    public void Record_Generated_Moves_To_Generated_And_Sets_Hash()
    {
        var report = Fresh();

        report.RecordGenerated("blob://x/y", "abc123", new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));

        report.Status.ShouldBe(ReportStatus.Generated);
        report.ContentHash.ShouldBe("abc123");
        report.StorageRef.ShouldBe("blob://x/y");
    }

    [Fact]
    public void Record_Generated_Raises_Integration_Event()
    {
        var report = Fresh();

        report.RecordGenerated("blob://x", "hash", DateTime.UtcNow);

        report.IntegrationEvents.ShouldContain(e => e is SessionReportGeneratedIntegrationEvent);
    }

    [Fact]
    public void Mark_Delivered_Only_From_Generated()
    {
        var report = Fresh();

        Should.Throw<InvalidOperationException>(() => report.MarkDelivered(DateTime.UtcNow));
    }

    [Fact]
    public void Generated_Then_Delivered_Captures_The_Delivery_Time()
    {
        var report = Fresh();
        report.RecordGenerated("blob", "hash", DateTime.UtcNow);

        var deliveredAt = new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc);
        report.MarkDelivered(deliveredAt);

        report.Status.ShouldBe(ReportStatus.Delivered);
        report.DeliveredAtUtc.ShouldBe(deliveredAt);
    }

    [Fact]
    public void Record_Failure_Captures_The_Reason()
    {
        var report = Fresh();

        report.RecordFailure("TemplateBindingException");

        report.Status.ShouldBe(ReportStatus.Failed);
        report.FailureReason.ShouldBe("TemplateBindingException");
    }
}

