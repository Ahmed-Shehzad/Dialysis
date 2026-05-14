namespace Dialysis.EHR.Core;

/// <summary>
/// EHR-specific UTC clock. Handlers depend on this rather than <see cref="DateTime.UtcNow"/>
/// so the module can be put on a fake clock in tests without leaking into other modules.
/// </summary>
public interface IEhrClock
{
    DateTime UtcNow { get; }
}
