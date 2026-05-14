namespace Dialysis.EHR.Core;

/// <summary>
/// Production <see cref="IEhrClock"/> backed by <see cref="DateTime.UtcNow"/>.
/// Tests substitute their own implementation.
/// </summary>
public sealed class EhrClock : IEhrClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
