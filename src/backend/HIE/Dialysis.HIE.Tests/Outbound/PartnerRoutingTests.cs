using Dialysis.HIE.Outbound;
using Dialysis.HIE.Outbound.Partners;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class PartnerRoutingTests
{
    [Fact]
    public void Falls_Back_To_Default_When_No_Routing_Partners()
    {
        var router = new ConfiguredPartnerRouter(
            Options.Create(new OutboundOptions { DefaultPartnerId = "default" }));

        router.ResolvePartners(Guid.NewGuid(), "clinical.note").ShouldBe(["default"]);
    }

    [Fact]
    public void Returns_Configured_Routing_Partners()
    {
        var options = new OutboundOptions { DefaultPartnerId = "default" };
        options.RoutingPartners.Add("partner-a");
        options.RoutingPartners.Add("partner-b");
        var router = new ConfiguredPartnerRouter(Options.Create(options));

        router.ResolvePartners(Guid.NewGuid(), "clinical.note").ShouldBe(["partner-a", "partner-b"]);
    }
}
