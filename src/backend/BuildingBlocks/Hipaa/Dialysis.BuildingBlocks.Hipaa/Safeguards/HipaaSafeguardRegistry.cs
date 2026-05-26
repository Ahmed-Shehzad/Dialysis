using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Hipaa.Safeguards;

/// <summary>
/// Snapshot of every registered <see cref="IHipaaSafeguardCheck"/> evaluated at request time. The
/// dashboard endpoint serialises this verbatim.
/// </summary>
public sealed record HipaaSafeguardSnapshot(
    DateTimeOffset EvaluatedAt,
    IReadOnlyList<HipaaSafeguardSnapshotEntry> Safeguards);

public sealed record HipaaSafeguardSnapshotEntry(
    string Id,
    string Name,
    HipaaSafeguardCategory Category,
    string SecurityRuleCitation,
    HipaaSafeguardStatus Status,
    string Evidence);

/// <summary>
/// Aggregates registered <see cref="IHipaaSafeguardCheck"/> implementations and runs them on
/// demand. Used by both the dashboard endpoint and the CI regression test (see
/// <c>HipaaSafeguardRegressionTests</c>) — that test asserts every check returns
/// <see cref="HipaaSafeguardStatus.Active"/> in the integration test composition so a regression
/// (e.g. someone removes HSTS) fails the build.
/// </summary>
public sealed class HipaaSafeguardRegistry(IEnumerable<IHipaaSafeguardCheck> checks)
{
    private readonly ConcurrentDictionary<string, IHipaaSafeguardCheck> _byId =
        new(checks.ToDictionary(c => c.Id, c => c, StringComparer.Ordinal), StringComparer.Ordinal);

    public HipaaSafeguardSnapshot Evaluate() => new(
        DateTimeOffset.UtcNow,
        _byId.Values
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .Select(c =>
            {
                var report = c.Evaluate();
                return new HipaaSafeguardSnapshotEntry(c.Id, c.Name, c.Category, c.SecurityRuleCitation, report.Status, report.Evidence);
            })
            .ToList());

    public IReadOnlyCollection<string> RegisteredIds => _byId.Keys.ToList();
}
