namespace Dialysis.HIS.Integration.DeviceRegistry;

/// <summary>
/// In-memory <see cref="IDeviceTypeCatalog"/> built from a fixed set of <see cref="DeviceType"/>s.
/// The composition layer constructs it from configuration (<c>His:DeviceRegistry:DeviceTypes</c>),
/// falling back to <see cref="Default"/> when none are configured — so the platform ships with the
/// dialysis machine plus the common RPM classes while remaining open to config-only additions.
/// </summary>
public sealed class DeviceTypeCatalog : IDeviceTypeCatalog
{
    private readonly IReadOnlyDictionary<string, DeviceType> _byCode;

    /// <summary>Builds a catalog from the supplied device types (later duplicates by code win).</summary>
    public DeviceTypeCatalog(IEnumerable<DeviceType> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        var map = new Dictionary<string, DeviceType>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in types)
        {
            if (!string.IsNullOrWhiteSpace(type.Code))
            {
                map[type.Code.Trim()] = type;
            }
        }

        _byCode = map;
    }

    /// <summary>
    /// The seed set: the dialysis machine the platform was built around plus the common RPM device
    /// classes (pulse-ox, weight scale, glucose meter, blood-pressure cuff, thermometer).
    /// </summary>
    public static IReadOnlyList<DeviceType> Default { get; } =
    [
        new("dialysis-machine", "Hemodialysis machine", "dialysis"),
        new("pulse-oximeter", "Pulse oximeter", "vitals", "%"),
        new("weight-scale", "Weight scale", "vitals", "kg"),
        new("glucose-meter", "Blood glucose meter", "vitals", "mg/dL"),
        new("blood-pressure-monitor", "Blood pressure monitor", "vitals", "mmHg"),
        new("thermometer", "Thermometer", "vitals", "Cel"),
    ];

    /// <inheritdoc />
    public IReadOnlyCollection<DeviceType> All =>
        [.. _byCode.Values.OrderBy(t => t.Code, StringComparer.Ordinal)];

    /// <inheritdoc />
    public DeviceType? Find(string code) =>
        string.IsNullOrWhiteSpace(code) ? null : _byCode.GetValueOrDefault(code.Trim());
}
