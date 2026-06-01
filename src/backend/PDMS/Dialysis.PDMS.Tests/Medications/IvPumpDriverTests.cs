using System.Text;
using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.Medications.IvPumps;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Medications;

/// <summary>
/// Each vendor driver gets one parse-correctness test against a representative payload.
/// Drivers are pure (no DI), so we don't need a host — just byte-array → reading.
/// </summary>
public sealed class IvPumpDriverTests
{
    [Fact]
    public async Task Bd_Alaris_Cqi_Parses_Progress_Payload_Async()
    {
        var driver = new BdAlarisCqiDriver();
        var payload = Encoding.UTF8.GetBytes("""
            {
              "device": { "id": "ALARIS-CH4-PUMP-7" },
              "event": "INFUSION_PROGRESS",
              "sequence": 1421,
              "timestamp": "2026-06-01T12:34:56Z",
              "infusion": {
                "programmedRateMlH": 100.0,
                "actualRateMlH": 99.8,
                "programmedVolumeMl": 250.0,
                "infusedVolumeMl": 142.0,
                "drug": { "rxnorm": "1234", "name": "Heparin" }
              }
            }
            """);

        var reading = await driver.ParseAsync(payload, CancellationToken.None);

        reading.VendorCode.ShouldBe("bd-alaris");
        reading.PumpDeviceId.ShouldBe("ALARIS-CH4-PUMP-7");
        reading.Kind.ShouldBe(IvPumpReadingKind.Progress);
        reading.SequenceNumber.ShouldBe(1421);
        reading.ProgrammedRateMlPerHour.ShouldBe(100.0m);
        reading.ActualRateMlPerHour.ShouldBe(99.8m);
        reading.InfusedVolumeMl.ShouldBe(142.0m);
        reading.MedicationCode.ShouldBe("1234");
        reading.MedicationCodeSystem.ShouldBe("http://www.nlm.nih.gov/research/umls/rxnorm");
    }

    [Fact]
    public async Task Bd_Alaris_Cqi_Recognises_Critical_Alarm_Async()
    {
        var driver = new BdAlarisCqiDriver();
        var payload = Encoding.UTF8.GetBytes("""
            {
              "device": { "id": "ALARIS-CH4-PUMP-7" },
              "event": "ALARM",
              "sequence": 1500,
              "timestamp": "2026-06-01T12:35:00Z",
              "alarm": { "code": "OCCLUSION_DISTAL", "text": "Distal occlusion detected.", "severity": "CRITICAL" }
            }
            """);

        var reading = await driver.ParseAsync(payload, CancellationToken.None);

        reading.Kind.ShouldBe(IvPumpReadingKind.Alarm);
        reading.AlarmCode.ShouldBe("OCCLUSION_DISTAL");
        reading.AlarmSeverity.ShouldBe(IvPumpAlarmSeverity.Critical);
    }

    [Fact]
    public async Task Baxter_Sigma_Parses_Drug_Library_Payload_Async()
    {
        var driver = new BaxterSigmaDriver();
        var payload = Encoding.UTF8.GetBytes("""
            {
              "deviceId": "SIGMA-CH4-PUMP-3",
              "eventType": "INFUSION_PROGRESS",
              "seq": 142,
              "ts": "2026-06-01T12:34:56Z",
              "rate": { "programmedMlPerHr": 100.0, "actualMlPerHr": 99.5 },
              "volume": { "programmedMl": 250.0, "infusedMl": 142.0 },
              "drug": { "atc": "B01AB01", "name": "Heparin" }
            }
            """);

        var reading = await driver.ParseAsync(payload, CancellationToken.None);

        reading.VendorCode.ShouldBe("baxter-sigma");
        reading.PumpDeviceId.ShouldBe("SIGMA-CH4-PUMP-3");
        reading.Kind.ShouldBe(IvPumpReadingKind.Progress);
        reading.MedicationCode.ShouldBe("B01AB01");
        reading.MedicationCodeSystem.ShouldBe("http://www.whocc.no/atc");
        reading.InfusedVolumeMl.ShouldBe(142.0m);
    }

    [Fact]
    public async Task Hospira_Plum_360_Parses_Snake_Case_Payload_Async()
    {
        var driver = new HospiraPlum360Driver();
        var payload = Encoding.UTF8.GetBytes("""
            {
              "pump_id": "PLUM360-CH5-PUMP-2",
              "kind": "progress",
              "ts": "2026-06-01T12:34:56Z",
              "programmed_rate_ml_h": 100.0,
              "actual_rate_ml_h": 99.7,
              "programmed_volume_ml": 250.0,
              "infused_volume_ml": 142.0,
              "drug_rxnorm": "1234"
            }
            """);

        var reading = await driver.ParseAsync(payload, CancellationToken.None);

        reading.VendorCode.ShouldBe("plum-360");
        reading.PumpDeviceId.ShouldBe("PLUM360-CH5-PUMP-2");
        reading.Kind.ShouldBe(IvPumpReadingKind.Progress);
        reading.ProgrammedRateMlPerHour.ShouldBe(100.0m);
        reading.ActualRateMlPerHour.ShouldBe(99.7m);
        reading.MedicationCode.ShouldBe("1234");
    }

    [Fact]
    public async Task Pcd04_Normalised_Parses_Hl7v2_Message_Async()
    {
        var driver = new Pcd04NormalisedDriver();
        // PCD-04 message: MSH + PRT (device) + OBX rows for rate / volume.
        var hl7 = string.Join('\r',
            "MSH|^~\\&|PCD04Pump|FAC|PDMS|FAC|20260601123456||ORU^R40^ORU_R40|1500|P|2.5",
            "PRT|1|||||||||PUMP-PCD-CH6-1",
            "OBX|1|NM|69871-9^IV programmed rate^LN||100.0|mL/h",
            "OBX|2|NM|69872-7^IV programmed volume^LN||250.0|mL",
            "OBX|3|NM|69869-3^IV infusion rate^LN||99.8|mL/h",
            "OBX|4|NM|69870-1^IV infused volume^LN||142.0|mL");
        var payload = Encoding.UTF8.GetBytes(hl7);

        var reading = await driver.ParseAsync(payload, CancellationToken.None);

        reading.VendorCode.ShouldBe("pcd04");
        reading.PumpDeviceId.ShouldBe("PUMP-PCD-CH6-1");
        reading.SequenceNumber.ShouldBe(1500);
        reading.ProgrammedRateMlPerHour.ShouldBe(100.0m);
        reading.ProgrammedVolumeMl.ShouldBe(250.0m);
        reading.ActualRateMlPerHour.ShouldBe(99.8m);
        reading.InfusedVolumeMl.ShouldBe(142.0m);
    }

    [Fact]
    public async Task Pcd04_Driver_Throws_When_Device_Id_Missing_Async()
    {
        var driver = new Pcd04NormalisedDriver();
        var hl7 = "MSH|^~\\&|PCD04Pump|FAC|PDMS|FAC|20260601123456||ORU^R40^ORU_R40|MSGID-1500|P|2.5";
        var payload = Encoding.UTF8.GetBytes(hl7);

        await Should.ThrowAsync<FormatException>(() => driver.ParseAsync(payload, CancellationToken.None));
    }
}
