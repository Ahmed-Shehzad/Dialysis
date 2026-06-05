using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Integration.DeviceRegistry;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>Domain coverage for the RPM device registry aggregate + the device-type catalog.</summary>
public sealed class DeviceRegistryTests
{
    private static readonly DateTime _now = new(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc);

    private static Device Register() =>
        Device.Register("OX-001", "pulse-oximeter", "Masimo", "Rad-97", "SN-12345", null, _now);

    [Fact]
    public void Register_Starts_In_Registered_State()
    {
        var device = Register();

        device.DeviceId.ShouldBe("OX-001");
        device.DeviceTypeCode.ShouldBe("pulse-oximeter");
        device.Status.ShouldBe(DeviceStatus.Registered);
        device.PatientId.ShouldBeNull();
        device.LastSeenAtUtc.ShouldBeNull();
        device.CanReport.ShouldBeTrue();
    }

    [Fact]
    public void Register_Requires_Device_Id_And_Type()
    {
        Should.Throw<DomainException>(() => Device.Register(" ", "pulse-oximeter", null, null, null, null, _now));
        Should.Throw<DomainException>(() => Device.Register("OX-1", " ", null, null, null, null, _now));
    }

    [Fact]
    public void Bind_Sets_Patient_And_Session()
    {
        var device = Register();
        var patientId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        device.BindToPatient(patientId, sessionId);

        device.PatientId.ShouldBe(patientId);
        device.SessionId.ShouldBe(sessionId);
    }

    [Fact]
    public void RecordSeen_Promotes_Registered_To_Active_And_Stamps_Last_Seen()
    {
        var device = Register();

        device.RecordSeen(_now);

        device.Status.ShouldBe(DeviceStatus.Active);
        device.LastSeenAtUtc.ShouldBe(_now);
    }

    [Fact]
    public void Suspended_Device_Rejects_Reporting_And_Binding()
    {
        var device = Register();
        device.Suspend();

        device.CanReport.ShouldBeFalse();
        Should.Throw<DomainException>(() => device.RecordSeen(_now));
        Should.Throw<DomainException>(() => device.BindToPatient(Guid.NewGuid(), null));

        device.Activate();
        device.Status.ShouldBe(DeviceStatus.Active);
        device.CanReport.ShouldBeTrue();
    }

    [Fact]
    public void Retired_Device_Is_Terminal()
    {
        var device = Register();
        device.BindToPatient(Guid.NewGuid(), null);
        device.Retire();

        device.Status.ShouldBe(DeviceStatus.Retired);
        device.PatientId.ShouldBeNull();
        device.CanReport.ShouldBeFalse();
        Should.Throw<DomainException>(() => device.Activate());
        Should.Throw<DomainException>(() => device.RecordSeen(_now));
    }

    [Fact]
    public void Catalog_Default_Includes_Dialysis_Machine_And_Rpm_Classes()
    {
        var catalog = new DeviceTypeCatalog(DeviceTypeCatalog.Default);

        catalog.Find("pulse-oximeter").ShouldNotBeNull();
        catalog.Find("PULSE-OXIMETER").ShouldNotBeNull(); // case-insensitive
        catalog.Find("dialysis-machine").ShouldNotBeNull();
        catalog.Find("not-a-device").ShouldBeNull();
        catalog.All.Count.ShouldBe(DeviceTypeCatalog.Default.Count);
    }

    [Fact]
    public void Catalog_Is_Config_Driven_Open_To_New_Classes()
    {
        var catalog = new DeviceTypeCatalog([new DeviceType("ecg-patch", "Wearable ECG patch", "vitals", "mV")]);

        catalog.Find("ecg-patch").ShouldNotBeNull();
        catalog.Find("pulse-oximeter").ShouldBeNull(); // only what was configured
    }
}
