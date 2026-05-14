using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Scheduling.Domain.ValueObjects;

/// <summary>
/// Inclusive-start, exclusive-end time window for an appointment, enforced as Start &lt; End.
/// </summary>
public sealed class AppointmentSlot : ValueObject
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public AppointmentSlot(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("AppointmentSlot start/end must be UTC.");
        if (startUtc >= endUtc)
            throw new DomainException("AppointmentSlot Start must be earlier than End.");
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartUtc;
        yield return EndUtc;
    }
}
