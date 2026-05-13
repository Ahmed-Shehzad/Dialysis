using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

public enum VascularAccessKind
{
    ArteriovenousFistula = 1,
    ArteriovenousGraft = 2,
    CentralVenousCatheter = 3,
    PeritonealCatheter = 4,
}

public sealed class VascularAccess : ValueObject
{
    public VascularAccessKind Kind { get; }

    public string Site { get; }

    public DateOnly EstablishedOn { get; }

    public VascularAccess(VascularAccessKind kind, string site, DateOnly establishedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(site);
        Kind = kind;
        Site = site.Trim();
        EstablishedOn = establishedOn;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return Site;
        yield return EstablishedOn;
    }
}
