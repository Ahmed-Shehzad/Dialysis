using Dialysis.SmartConnect.TreatmentReport;
using Xunit;

namespace Dialysis.SmartConnect.Tests.TreatmentReport;

/// <summary>
/// Conformance for the IG §6.2 PCD-01 treatment-report builder. The §5 closing
/// paragraph says: "Any setting value that is sent from the EMR to the Dialysis Machine
/// will be sent back in the PCD-01 messages" with OBX-17 indicating RSET / MSET / ASET.
/// These tests cover that round-trip end-to-end: settings carried as
/// <see cref="ObservationFrame"/> + provenance → OBX rows with OBX-17 populated.
/// </summary>
public sealed class Hl7V2OruR40BuilderTests
{
    private static readonly DateTime _observedAt = new(2026, 5, 22, 14, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime _nowUtc = new(2026, 5, 22, 14, 30, 1, DateTimeKind.Utc);

    private static MachineIdentity Machine() => new(
        ApplicationName: "ACME_Dialysis_Machine",
        DeviceIdentifier: "080019FFFE3ED02D",
        IdentifierAssigningAuthority: "EUI-64");

    [Fact]
    public void Builder_Emits_R40_Header_With_Machine_Identity()
    {
        var frame = new TreatmentReportFrame(
            Machine: Machine(),
            PatientIdentifier: "MRN-9001",
            ObservedAtUtc: _observedAt,
            Observations: []);

        var wire = Hl7V2OruR40Builder.Build(frame, "MSG-42", _nowUtc);

        Assert.Contains("MSH|^~\\&|ACME_Dialysis_Machine^080019FFFE3ED02D^EUI-64|", wire);
        Assert.Contains("|ORU^R40^ORU_R40|MSG-42|P|2.6|", wire);
        Assert.Contains("PID|||MRN-9001^^^^MR", wire);
        // OBR-3 = placer order with timestamp + app + device, OBR-4 = MDS code.
        Assert.Contains("OBR|1||20260522143000+0000^ACME_Dialysis_Machine^080019FFFE3ED02D^EUI-64|70929^MDC_DEV_HDIALY_MACHINE_MDS^MDC", wire);
    }

    [Fact]
    public void Builder_Emits_Obx_17_Rset_For_Remote_Setting()
    {
        // A blood-flow-rate setting that came from the EMR and is unchanged on the
        // machine should round-trip with OBX-17 = RSET^remote-setting^MDC per IG §5.
        var frame = new TreatmentReportFrame(
            Machine: Machine(),
            PatientIdentifier: "MRN-1",
            ObservedAtUtc: _observedAt,
            Observations:
            [
                new ObservationFrame(
                    ValueType: "NM",
                    ObservationId: "16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC",
                    ContainmentPath: "1.1.3.1",
                    Value: "250",
                    Units: "ml/min^ml/min^UCUM",
                    Source: ObservationSource.RemoteSetting),
            ]);

        var wire = Hl7V2OruR40Builder.Build(frame, "MSG-1", _nowUtc);

        Assert.Contains(
            "OBX|1|NM|16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC|1.1.3.1|250|ml/min^ml/min^UCUM|||||F||||20260522143000+0000||RSET^remote-setting^MDC",
            wire);
    }

    [Fact]
    public void Builder_Emits_Mset_When_Setting_Locally_Overridden()
    {
        var frame = new TreatmentReportFrame(
            Machine: Machine(),
            PatientIdentifier: "MRN-2",
            ObservedAtUtc: _observedAt,
            Observations:
            [
                new ObservationFrame(
                    ValueType: "NM",
                    ObservationId: "16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC",
                    ContainmentPath: "1.1.3.1",
                    Value: "275",
                    Units: "ml/min^ml/min^UCUM",
                    Source: ObservationSource.ManualSetting),
            ]);

        var wire = Hl7V2OruR40Builder.Build(frame, "MSG-2", _nowUtc);

        Assert.Contains("||MSET^manual-setting^MDC", wire);
        Assert.DoesNotContain("RSET", wire);
    }

    [Fact]
    public void Builder_Emits_Aset_For_Auto_Controlled_Settings()
    {
        var frame = new TreatmentReportFrame(
            Machine: Machine(),
            PatientIdentifier: "MRN-3",
            ObservedAtUtc: _observedAt,
            Observations:
            [
                new ObservationFrame(
                    ValueType: "NM",
                    ObservationId: "16936252^MDC_HDIALY_UF_RATE_SETTING^MDC",
                    ContainmentPath: "1.1.5.2",
                    Value: "380",
                    Units: "ml/h^ml/h^UCUM",
                    Source: ObservationSource.AutoSetting),
            ]);

        var wire = Hl7V2OruR40Builder.Build(frame, "MSG-3", _nowUtc);

        Assert.Contains("||ASET^auto-setting^MDC", wire);
    }

    [Fact]
    public void Builder_Distinguishes_Measurements_From_Settings()
    {
        // Measurement (NIBP cuff, manually triggered) → MMEAS.
        // Container row (no value, no source) → empty OBX-17.
        var frame = new TreatmentReportFrame(
            Machine: Machine(),
            PatientIdentifier: "MRN-4",
            ObservedAtUtc: _observedAt,
            Observations:
            [
                new ObservationFrame("ST", "70929^MDC_DEV_HDIALY_MACHINE_MDS^MDC", "1", string.Empty, null, null),
                new ObservationFrame(
                    "NM",
                    "8480-6^Systolic blood pressure^LN",
                    "1.1.6.1",
                    "138",
                    "mm[Hg]^mm[Hg]^UCUM",
                    ObservationSource.ManualMeasurement),
            ]);

        var wire = Hl7V2OruR40Builder.Build(frame, "MSG-4", _nowUtc);

        // Container row: OBX-17 area ends after the timestamp + double-pipe, no source.
        Assert.Contains("OBX|1|ST|70929^MDC_DEV_HDIALY_MACHINE_MDS^MDC|1|||||||F||||20260522143000+0000||\r", wire);
        Assert.Contains("||MMEAS^manual-measurement^MDC", wire);
        Assert.DoesNotContain("AMEAS", wire);
    }

    [Fact]
    public void Builder_Container_Rows_Emit_Empty_Obx_17()
    {
        // A pure containment OBX (Source = null) must not gain a spurious OBX-17 token.
        var frame = new TreatmentReportFrame(
            Machine: Machine(),
            PatientIdentifier: "MRN-5",
            ObservedAtUtc: _observedAt,
            Observations:
            [
                new ObservationFrame("ST", "70947^MDC_DEV_HDIALY_BLOOD_PUMP_CHAN^MDC", "1.1.3", string.Empty, null, null),
            ]);

        var wire = Hl7V2OruR40Builder.Build(frame, "MSG-5", _nowUtc);

        Assert.EndsWith("20260522143000+0000||\r", wire);
        Assert.DoesNotContain("RSET", wire);
        Assert.DoesNotContain("MSET", wire);
        Assert.DoesNotContain("ASET", wire);
    }
}
