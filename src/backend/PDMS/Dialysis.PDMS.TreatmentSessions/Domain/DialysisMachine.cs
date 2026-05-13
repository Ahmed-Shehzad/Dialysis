using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

/// <summary>
/// A physical dialysis machine registered with the system. Identity is the manufacturer-issued serial number.
/// Registration is admin-time only — telemetry from an unregistered serial is rejected (AE) at the HL7 layer.
/// </summary>
public sealed class DialysisMachine : AggregateRoot<Guid>
{
    private DialysisMachine()
    {
    }

    public DialysisMachine(Guid id) : base(id)
    {
    }

    public string SerialNumber { get; private set; } = default!;

    public string? VendorCode { get; private set; }

    public string? ModelCode { get; private set; }

    public DateTime? LastSeenUtc { get; private set; }

    /// <summary>Session this machine is currently bound to. Nullable when idle.</summary>
    public Guid? CurrentSessionId { get; private set; }

    public static DialysisMachine Register(Guid id, string serialNumber, string? vendorCode, string? modelCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
        if (serialNumber.Length > 64)
            throw new ArgumentException("Serial number too long.", nameof(serialNumber));

        return new DialysisMachine(id)
        {
            SerialNumber = serialNumber.Trim(),
            VendorCode = string.IsNullOrWhiteSpace(vendorCode) ? null : vendorCode.Trim(),
            ModelCode = string.IsNullOrWhiteSpace(modelCode) ? null : modelCode.Trim(),
        };
    }

    public void Touch(DateTime seenAtUtc)
    {
        if (LastSeenUtc is null || seenAtUtc > LastSeenUtc)
            LastSeenUtc = seenAtUtc;
    }

    public void BindToSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Session required.", nameof(sessionId));
        CurrentSessionId = sessionId;
    }

    public void ReleaseFromSession() => CurrentSessionId = null;
}
