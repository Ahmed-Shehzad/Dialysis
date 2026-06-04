namespace Dialysis.BuildingBlocks.DataProtection.LawfulBases;

/// <summary>Default in-memory implementation. Composition root constructs once at startup.</summary>
public sealed class LawfulBasisRegistry : ILawfulBasisRegistry
{
    private readonly ProcessingActivity[] _activities;

    public LawfulBasisRegistry(string moduleSlug, IEnumerable<ProcessingActivity> activities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);
        ArgumentNullException.ThrowIfNull(activities);
        ModuleSlug = moduleSlug;
        _activities = [.. activities];
    }

    public string ModuleSlug { get; }

    public IReadOnlyList<ProcessingActivity> Activities => _activities;

    public bool IsAuthorised(LawfulBasis basis, DataCategory categories) =>
        _activities.Any(a => a.Basis == basis && (a.Categories & categories) == categories);
}
