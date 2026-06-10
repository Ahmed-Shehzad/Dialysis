namespace Dialysis.BuildingBlocks.DataProtection.LawfulBases;

/// <summary>
/// Decorates a command / query / event handler with the GDPR lawful basis it processes data
/// under and the data categories it touches. The `LawfulBasisGuardBehavior` interceptor reads
/// the attribute at execution time and refuses to invoke the handler if no matching basis is
/// registered for the module — see <see cref="ILawfulBasisRegistry"/>.
///
/// Apply to:
/// <list type="bullet">
///   <item>Commands that mutate identifiable health data (always required).</item>
///   <item>Queries that surface identifiable health data (always required).</item>
///   <item>Integration-event consumers that read identifiable health data from a sibling module.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LawfulBasisAttribute : Attribute
{
    public LawfulBasisAttribute(LawfulBasis basis, DataCategory categories)
    {
        Basis = basis;
        Categories = categories;
    }

    public LawfulBasis Basis { get; }

    public DataCategory Categories { get; }

    /// <summary>
    /// Optional free-text purpose statement, surfaced in the RoPA document. When omitted the
    /// RoPA generator falls back to the handler's class name.
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Optional retention key — references a window registered with
    /// <see cref="Retention.IRetentionSchedule"/>. When omitted the handler inherits its
    /// module's default retention.
    /// </summary>
    public string? RetentionKey { get; init; }
}
