using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Integration.DeviceRegistry;

/// <summary>Lifecycle of a registered device.</summary>
public enum DeviceStatus
{
    /// <summary>Registered in the catalog but has not yet reported a reading.</summary>
    Registered = 0,

    /// <summary>Has reported at least one reading and is currently accepted.</summary>
    Active = 1,

    /// <summary>Temporarily blocked (e.g. calibration overdue, suspected fault) — readings rejected.</summary>
    Suspended = 2,

    /// <summary>Permanently decommissioned — readings rejected and it cannot be re-activated.</summary>
    Retired = 3,
}

/// <summary>
/// Registry entry for a remote-patient-monitoring device — the identity, provenance, and
/// patient/session binding that turns an anonymous <c>DeviceId</c> string on an ingest payload into
/// a known, governed source. Ingestion resolves the reading's device here, checks it may report
/// (Active/Registered, not Suspended/Retired), and binds the reading to the device's current
/// patient/session. <see cref="DeviceId"/> is the stable external identifier (serial / gateway id)
/// the device stamps on every reading.
/// </summary>
public sealed class Device : AggregateRoot<Guid>
{
    private Device()
    {
    }

    private Device(Guid id) : base(id)
    {
    }

    /// <summary>Stable external identifier the device stamps on its readings (serial / gateway id).</summary>
    public string DeviceId { get; private set; } = null!;

    /// <summary>Device-type code from <see cref="IDeviceTypeCatalog"/> (e.g. <c>pulse-oximeter</c>).</summary>
    public string DeviceTypeCode { get; private set; } = null!;

    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNumber { get; private set; }

    /// <summary>Currently bound patient; null until the device is assigned.</summary>
    public Guid? PatientId { get; private set; }

    /// <summary>Currently bound treatment session, when the device is chairside; otherwise null.</summary>
    public Guid? SessionId { get; private set; }

    public DeviceStatus Status { get; private set; } = DeviceStatus.Registered;

    /// <summary>Next calibration due date; a reading past this date is provenance-suspect.</summary>
    public DateTime? CalibrationDueUtc { get; private set; }

    public DateTime RegisteredAtUtc { get; private set; }

    /// <summary>Timestamp of the most recent accepted reading; null until the first.</summary>
    public DateTime? LastSeenAtUtc { get; private set; }

    /// <summary>Registers a new device against a validated type code.</summary>
    public static Device Register(
        string deviceId,
        string deviceTypeCode,
        string? manufacturer,
        string? model,
        string? serialNumber,
        DateTime? calibrationDueUtc,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new DomainException("Device requires an external device id.");
        if (string.IsNullOrWhiteSpace(deviceTypeCode))
            throw new DomainException("Device requires a device-type code.");

        return new Device(Guid.CreateVersion7())
        {
            DeviceId = deviceId.Trim(),
            DeviceTypeCode = deviceTypeCode.Trim(),
            Manufacturer = Clean(manufacturer),
            Model = Clean(model),
            SerialNumber = Clean(serialNumber),
            CalibrationDueUtc = calibrationDueUtc,
            Status = DeviceStatus.Registered,
            RegisteredAtUtc = nowUtc,
        };
    }

    /// <summary>Binds (or re-binds) the device to a patient and optional treatment session.</summary>
    public void BindToPatient(Guid patientId, Guid? sessionId)
    {
        if (patientId == Guid.Empty)
            throw new DomainException("A device must bind to a real patient.");
        EnsureUsable("bound to a patient");
        PatientId = patientId;
        SessionId = sessionId == Guid.Empty ? null : sessionId;
    }

    /// <summary>Clears any patient/session binding (device returned to the pool).</summary>
    public void Unbind()
    {
        EnsureUsable("unbound");
        PatientId = null;
        SessionId = null;
    }

    /// <summary>
    /// Records that an accepted reading arrived: stamps <see cref="LastSeenAtUtc"/> and promotes a
    /// freshly <see cref="DeviceStatus.Registered"/> device to <see cref="DeviceStatus.Active"/>.
    /// Throws when the device may not report (Suspended/Retired) — ingestion turns this into a reject.
    /// </summary>
    public void RecordSeen(DateTime nowUtc)
    {
        EnsureUsable("reporting a reading");
        if (Status == DeviceStatus.Registered)
            Status = DeviceStatus.Active;
        LastSeenAtUtc = nowUtc;
    }

    /// <summary>Whether the device is allowed to report readings right now.</summary>
    public bool CanReport => Status is DeviceStatus.Registered or DeviceStatus.Active;

    /// <summary>Temporarily blocks the device from reporting (reversible via <see cref="Activate"/>).</summary>
    public void Suspend()
    {
        if (Status == DeviceStatus.Retired)
            throw new DomainException("A retired device cannot be suspended.");
        Status = DeviceStatus.Suspended;
    }

    /// <summary>Re-activates a suspended device.</summary>
    public void Activate()
    {
        if (Status == DeviceStatus.Retired)
            throw new DomainException("A retired device cannot be re-activated.");
        Status = DeviceStatus.Active;
    }

    /// <summary>Permanently decommissions the device.</summary>
    public void Retire()
    {
        Status = DeviceStatus.Retired;
        PatientId = null;
        SessionId = null;
    }

    /// <summary>Updates the calibration due date (e.g. after a service visit).</summary>
    public void SetCalibrationDue(DateTime? calibrationDueUtc) => CalibrationDueUtc = calibrationDueUtc;

    private void EnsureUsable(string action)
    {
        if (Status == DeviceStatus.Retired)
            throw new DomainException($"A retired device cannot be {action}.");
        if (Status == DeviceStatus.Suspended)
            throw new DomainException($"A suspended device cannot be {action}.");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
