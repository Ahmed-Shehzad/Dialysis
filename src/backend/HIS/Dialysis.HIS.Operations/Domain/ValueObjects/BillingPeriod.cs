using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Operations.Domain.ValueObjects;

/// <summary>
/// Inclusive-start, inclusive-end calendar period used to scope a <see cref="BillingExportJob"/>.
/// Invariant: <c>Start &lt; End</c>.
/// </summary>
public sealed class BillingPeriod : ValueObject
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public BillingPeriod(DateOnly start, DateOnly end)
    {
        if (start >= end)
            throw new DomainException($"BillingPeriod requires Start ({start:O}) < End ({end:O}).");
        Start = start;
        End = end;
    }

    public override string ToString() => $"{Start:O}..{End:O}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
