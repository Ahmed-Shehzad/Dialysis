using Dialysis.Treatment.Application.Domain.Hl7;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Shouldly;

namespace Dialysis.Treatment.Tests;

public sealed class ChannelPresenceByModeTests
{
    [Fact]
    public void GetChannelsForMode_Idle_ReturnsOnlyMachine()
    {
        var channels = ChannelPresenceByMode.GetChannelsForMode(ModeOfOperation.Idle);
        channels.ShouldBe([ChannelPresenceByMode.Machine]);
    }

    [Fact]
    public void GetChannelsForMode_Service_ReturnsOnlyMachine()
    {
        var channels = ChannelPresenceByMode.GetChannelsForMode(ModeOfOperation.Service);
        channels.ShouldBe([ChannelPresenceByMode.Machine]);
    }

    [Fact]
    public void GetChannelsForModality_HD_IncludesBloodPumpAndUf_ExcludesConvective()
    {
        var channels = ChannelPresenceByMode.GetChannelsForModality(TreatmentModality.Hemodialysis);
        channels.ShouldContain(ChannelPresenceByMode.BloodPump);
        channels.ShouldContain(ChannelPresenceByMode.Uf);
        channels.ShouldNotContain(ChannelPresenceByMode.Convective);
    }

    [Fact]
    public void GetChannelsForModality_HDF_IncludesConvective()
    {
        var channels = ChannelPresenceByMode.GetChannelsForModality(TreatmentModality.Hemodiafiltration);
        channels.ShouldContain(ChannelPresenceByMode.Convective);
    }

    [Fact]
    public void GetChannelsForModality_HF_ExcludesDialysate()
    {
        var channels = ChannelPresenceByMode.GetChannelsForModality(TreatmentModality.Hemofiltration);
        channels.ShouldNotContain(ChannelPresenceByMode.Dialysate);
        channels.ShouldContain(ChannelPresenceByMode.Convective);
    }

    [Fact]
    public void GetChannelsForModality_IUF_ExcludesDialysateAndConvective()
    {
        var channels = ChannelPresenceByMode.GetChannelsForModality(TreatmentModality.IsolatedUltrafiltration);
        channels.ShouldNotContain(ChannelPresenceByMode.Dialysate);
        channels.ShouldNotContain(ChannelPresenceByMode.Convective);
    }

    [Fact]
    public void IsChannelPresent_HD_BloodPump_ReturnsTrue()
    {
        ChannelPresenceByMode.IsChannelPresent(ChannelPresenceByMode.BloodPump, TreatmentModality.Hemodialysis)
            .ShouldBeTrue();
    }

    [Fact]
    public void IsChannelPresent_Idle_BloodPump_ReturnsFalse()
    {
        ChannelPresenceByMode.IsChannelPresent(ChannelPresenceByMode.BloodPump, ModeOfOperation.Idle)
            .ShouldBeFalse();
    }
}
