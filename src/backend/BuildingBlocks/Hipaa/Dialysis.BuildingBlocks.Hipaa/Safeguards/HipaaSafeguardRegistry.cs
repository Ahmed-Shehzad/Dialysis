using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Hipaa.Safeguards;

/// <summary>
/// Snapshot of every registered <see cref="IHipaaSafeguardCheck"/> evaluated at request time. The
/// dashboard endpoint serialises this verbatim.
/// </summary>
public sealed record HipaaSafeguardSnapshot
{
    /// <summary>
    /// Snapshot of every registered <see cref="IHipaaSafeguardCheck"/> evaluated at request time. The
    /// dashboard endpoint serialises this verbatim.
    /// </summary>
    public HipaaSafeguardSnapshot(DateTimeOffset EvaluatedAt,
        IReadOnlyList<HipaaSafeguardSnapshotEntry> Safeguards)
    {
        this.EvaluatedAt = EvaluatedAt;
        this.Safeguards = Safeguards;
    }
    public DateTimeOffset EvaluatedAt { get; init; }
    public IReadOnlyList<HipaaSafeguardSnapshotEntry> Safeguards { get; init; }
    public void Deconstruct(out DateTimeOffset EvaluatedAt, out IReadOnlyList<HipaaSafeguardSnapshotEntry> Safeguards)
    {
        EvaluatedAt = this.EvaluatedAt;
        Safeguards = this.Safeguards;
    }
}

public sealed record HipaaSafeguardSnapshotEntry
{
    public HipaaSafeguardSnapshotEntry(string Id,
        string Name,
        HipaaSafeguardCategory Category,
        string SecurityRuleCitation,
        HipaaSafeguardStatus Status,
        string Evidence)
    {
        this.Id = Id;
        this.Name = Name;
        this.Category = Category;
        this.SecurityRuleCitation = SecurityRuleCitation;
        this.Status = Status;
        this.Evidence = Evidence;
    }
    public string Id { get; init; }
    public string Name { get; init; }
    public HipaaSafeguardCategory Category { get; init; }
    public string SecurityRuleCitation { get; init; }
    public HipaaSafeguardStatus Status { get; init; }
    public string Evidence { get; init; }
    public void Deconstruct(out string Id, out string Name, out HipaaSafeguardCategory Category, out string SecurityRuleCitation, out HipaaSafeguardStatus Status, out string Evidence)
    {
        Id = this.Id;
        Name = this.Name;
        Category = this.Category;
        SecurityRuleCitation = this.SecurityRuleCitation;
        Status = this.Status;
        Evidence = this.Evidence;
    }
}

/// <summary>
/// Aggregates registered <see cref="IHipaaSafeguardCheck"/> implementations and runs them on
/// demand. Used by both the dashboard endpoint and the CI regression test (see
/// <c>HipaaSafeguardRegressionTests</c>) — that test asserts every check returns
/// <see cref="HipaaSafeguardStatus.Active"/> in the integration test composition so a regression
/// (e.g. someone removes HSTS) fails the build.
/// </summary>
public sealed class HipaaSafeguardRegistry
{
    private readonly ConcurrentDictionary<string, IHipaaSafeguardCheck> _byId;

    /// <summary>
    /// Aggregates registered <see cref="IHipaaSafeguardCheck"/> implementations and runs them on
    /// demand. Used by both the dashboard endpoint and the CI regression test (see
    /// <c>HipaaSafeguardRegressionTests</c>) — that test asserts every check returns
    /// <see cref="HipaaSafeguardStatus.Active"/> in the integration test composition so a regression
    /// (e.g. someone removes HSTS) fails the build.
    /// </summary>
    public HipaaSafeguardRegistry(IEnumerable<IHipaaSafeguardCheck> checks) => _byId = new ConcurrentDictionary<string, IHipaaSafeguardCheck>(checks.ToDictionary(c => c.Id, c => c, StringComparer.Ordinal), StringComparer.Ordinal);

    public HipaaSafeguardSnapshot Evaluate() => new(
        DateTimeOffset.UtcNow,
        [.. _byId.Values
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .Select(c =>
            {
                var report = c.Evaluate();
                return new HipaaSafeguardSnapshotEntry(c.Id, c.Name, c.Category, c.SecurityRuleCitation, report.Status, report.Evidence);
            })]);

    public IReadOnlyCollection<string> RegisteredIds => [.. _byId.Keys];
}
