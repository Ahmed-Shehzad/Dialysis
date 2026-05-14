namespace Dialysis.PDMS.Core;

/// <summary>
/// PDMS-specific UTC clock. Handlers depend on this rather than <see cref="DateTime.UtcNow"/>
/// so the module can be put on a fake clock in tests.
/// </summary>
public interface IPdmsClock
{
    DateTime UtcNow { get; }
}
