using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.PatientPortal.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.PatientPortal;

public sealed class AfterVisitSummaryTests
{
    private static readonly DateTime _now = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    private static AfterVisitSummary Draft()
    {
        var avs = AfterVisitSummary.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _now, Guid.NewGuid(), "We adjusted your dry weight.");
        avs.AddInstruction(Guid.NewGuid(), "Weigh yourself every morning");
        avs.AddFollowUp(Guid.NewGuid(), "Nephrology in 2 weeks");
        avs.AddResourceLink(Guid.NewGuid(), "Fluid management", "https://example.org/fluid");
        return avs;
    }

    [Fact]
    public void Draft_Holds_The_Authored_Lines()
    {
        var avs = Draft();
        avs.Status.ShouldBe(AfterVisitSummaryStatus.Draft);
        avs.Instructions.Count.ShouldBe(1);
        avs.FollowUps.Count.ShouldBe(1);
        avs.ResourceLinks.ShouldHaveSingleItem().Url.ShouldBe("https://example.org/fluid");
        avs.IntegrationEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Publish_Raises_The_Event_Once_And_Is_Idempotent()
    {
        var avs = Draft();

        avs.Publish(_now);
        avs.Publish(_now.AddHours(1));

        avs.Status.ShouldBe(AfterVisitSummaryStatus.Published);
        avs.PublishedAtUtc.ShouldBe(_now);
        avs.IntegrationEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<AfterVisitSummaryPublishedIntegrationEvent>();
    }

    [Fact]
    public void Cannot_Edit_After_Publish()
    {
        var avs = Draft();
        avs.Publish(_now);

        Should.Throw<InvalidOperationException>(() => avs.AddInstruction(Guid.NewGuid(), "too late"));
    }
}
