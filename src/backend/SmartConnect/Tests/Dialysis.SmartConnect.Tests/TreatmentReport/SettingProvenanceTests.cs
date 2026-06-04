using Dialysis.SmartConnect.TreatmentReport;
using Xunit;

namespace Dialysis.SmartConnect.Tests.TreatmentReport;

/// <summary>
/// Verifies the IG §5 one-way transition rule for setting provenance and the §8.2.5
/// OBX-17 wire-token mapping.
/// </summary>
public sealed class SettingProvenanceTests
{
    [Fact]
    public void New_Remote_Setting_Starts_As_Rset()
    {
        var prov = new SettingProvenance(ObservationSource.RemoteSetting);
        Assert.Equal(ObservationSource.RemoteSetting, prov.Current);
    }

    [Fact]
    public void Manual_Override_From_Rset_Flips_To_Mset()
    {
        var prov = new SettingProvenance(ObservationSource.RemoteSetting);
        prov.MarkManualOverride();
        Assert.Equal(ObservationSource.ManualSetting, prov.Current);
    }

    [Fact]
    public void Once_Manual_Always_Manual_Until_Auto()
    {
        var prov = new SettingProvenance(ObservationSource.RemoteSetting);
        prov.MarkManualOverride();
        // Re-applying manual is idempotent — no spurious bookkeeping.
        prov.MarkManualOverride();
        Assert.Equal(ObservationSource.ManualSetting, prov.Current);

        // Auto-controller may seize a manually-set setting.
        prov.MarkAutoOverride();
        Assert.Equal(ObservationSource.AutoSetting, prov.Current);
    }

    [Fact]
    public void Auto_Setting_Cannot_Drop_Back_To_Manual()
    {
        var prov = new SettingProvenance(ObservationSource.RemoteSetting);
        prov.MarkAutoOverride();
        Assert.Equal(ObservationSource.AutoSetting, prov.Current);
        Assert.Throws<InvalidOperationException>(() => prov.MarkManualOverride());
    }

    [Fact]
    public void Measurement_Source_Rejected_From_Provenance_Constructor()
    {
        Assert.Throws<ArgumentException>(() => new SettingProvenance(ObservationSource.AutoMeasurement));
        Assert.Throws<ArgumentException>(() => new SettingProvenance(ObservationSource.ManualMeasurement));
    }

    [Theory]
    [InlineData(ObservationSource.AutoMeasurement, "AMEAS", "AMEAS^auto-measurement^MDC")]
    [InlineData(ObservationSource.ManualMeasurement, "MMEAS", "MMEAS^manual-measurement^MDC")]
    [InlineData(ObservationSource.RemoteSetting, "RSET", "RSET^remote-setting^MDC")]
    [InlineData(ObservationSource.ManualSetting, "MSET", "MSET^manual-setting^MDC")]
    [InlineData(ObservationSource.AutoSetting, "ASET", "ASET^auto-setting^MDC")]
    public void Obx17_Token_Mapping_Matches_Ig_Spec(
        ObservationSource source, string expectedToken, string expectedCwe)
    {
        Assert.Equal(expectedToken, source.ToObx17Token());
        Assert.Equal(expectedCwe, source.ToObx17Cwe());
    }

    [Theory]
    [InlineData(ObservationSource.RemoteSetting, true)]
    [InlineData(ObservationSource.ManualSetting, true)]
    [InlineData(ObservationSource.AutoSetting, true)]
    [InlineData(ObservationSource.AutoMeasurement, false)]
    [InlineData(ObservationSource.ManualMeasurement, false)]
    public void Is_Setting_Discriminator_Matches_Ig_Categories(ObservationSource source, bool expected) => Assert.Equal(expected, source.IsSetting());
}
