namespace Dialysis.BuildingBlocks.DataProtection.LawfulBases;

/// <summary>
/// Per-module registry of legitimate processing activities. Each module's composition root
/// declares which (basis, categories) tuples it's authorised to invoke; the guard behaviour
/// rejects any command whose attribute doesn't match an entry.
///
/// Registry entries are immutable per process — they're set at startup and don't change at
/// runtime. The RoPA generator reads them to build the GDPR Art. 30 document.
/// </summary>
public interface ILawfulBasisRegistry
{
    /// <summary>The module slug this registry is scoped to (e.g. <c>"pdms"</c>).</summary>
    string ModuleSlug { get; }

    /// <summary>Every authorised processing activity, snapshot for RoPA emission.</summary>
    IReadOnlyList<ProcessingActivity> Activities { get; }

    /// <summary>
    /// <c>true</c> when the module has a registered activity that covers <paramref name="basis"/>
    /// and at least the categories in <paramref name="categories"/>. Used by the guard behaviour.
    /// </summary>
    bool IsAuthorised(LawfulBasis basis, DataCategory categories);
}

/// <summary>
/// One row in a module's lawful-basis registry. Surfaces in the RoPA document.
/// </summary>
public sealed record ProcessingActivity(
    string ActivityName,
    LawfulBasis Basis,
    DataCategory Categories,
    string Purpose,
    string? RetentionKey,
    IReadOnlyList<string> RecipientCategories);
