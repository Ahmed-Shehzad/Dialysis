namespace Dialysis.HIE.Inbound.Terminology;

/// <summary>
/// Repository for <see cref="AuthoredTerminologyResource"/>. The admin surface drives
/// add/find/list/remove; the catalog loader reads <see cref="ListActiveAsync"/> at startup.
/// </summary>
public interface IAuthoredTerminologyRepository
{
    void Add(AuthoredTerminologyResource resource);

    void Remove(AuthoredTerminologyResource resource);

    Task<AuthoredTerminologyResource?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Finds the row for a specific canonical (url, version).</summary>
    Task<AuthoredTerminologyResource?> FindByUrlVersionAsync(string url, string version, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuthoredTerminologyResource>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Every <c>active</c> row, for the startup catalog overlay.</summary>
    Task<IReadOnlyList<AuthoredTerminologyResource>> ListActiveAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
