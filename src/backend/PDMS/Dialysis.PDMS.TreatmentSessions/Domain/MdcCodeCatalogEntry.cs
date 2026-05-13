using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

public enum MdcCodeCategory
{
    Mds = 1,
    Vmd = 2,
    Channel = 3,
    Metric = 4,
    Setting = 5,
    Alarm = 6,
    Profile = 7,
}

/// <summary>
/// Static reference data: an ISO/IEEE 11073 MDC code recognized by the system. Codes outside this catalog
/// are still persisted (with <see cref="IsVendorSpecific"/> inferred from the private MDC range 0xF000–0xFFFF)
/// but won't have a human-readable display name. Seeded in Phase D from Tables 2–5 of the rev4 spec.
/// </summary>
public sealed class MdcCodeCatalogEntry : Entity<long>
{
    private MdcCodeCatalogEntry()
    {
    }

    public MdcCodeCatalogEntry(long code) : base(code)
    {
    }

    public string DisplayName { get; private set; } = default!;

    public MdcCodeCategory Category { get; private set; }

    public string? Units { get; private set; }

    public bool IsVendorSpecific { get; private set; }

    public static MdcCodeCatalogEntry Define(long code, string displayName, MdcCodeCategory category, string? units, bool isVendorSpecific)
    {
        if (code <= 0) throw new ArgumentOutOfRangeException(nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new MdcCodeCatalogEntry(code)
        {
            DisplayName = displayName.Trim(),
            Category = category,
            Units = string.IsNullOrWhiteSpace(units) ? null : units.Trim(),
            IsVendorSpecific = isVendorSpecific,
        };
    }
}
