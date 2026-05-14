namespace Dialysis.PDMS.Core;

/// <summary>
/// Production <see cref="IPdmsClock"/> backed by <see cref="DateTime.UtcNow"/>.
/// </summary>
public sealed class PdmsClock : IPdmsClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
