using Dialysis.PDMS.Reporting.Domain;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Persists rendered report bytes outside the relational DB — the aggregate carries only the
/// content hash + storage ref. Production hosts back this with S3 / Azure Blob; dev defaults
/// to a local-filesystem store.
/// </summary>
public interface IReportBlobStore
{
    /// <summary>Returns the storage-ref handle for the persisted body.</summary>
    Task<string> SaveAsync(Guid reportId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken);
}

/// <summary>Repository for the <see cref="Domain.SessionReport"/> aggregate.</summary>
public interface ISessionReportRepository
{
    /// <summary>Stages a new report; persisted by the consumer's unit of work.</summary>
    Task AddAsync(SessionReport report, CancellationToken cancellationToken);
    Task<SessionReport?> FindAsync(Guid reportId, CancellationToken cancellationToken);

    /// <summary>Returns every report already produced for a session — the consumer's idempotency key is <c>(SessionId, Kind)</c>.</summary>
    Task<IReadOnlyList<SessionReport>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken);
}

/// <summary>Repository for the operator-authored <see cref="Domain.ReportTemplate"/>.</summary>
public interface IReportTemplateRepository
{
    /// <summary>
    /// Resolves the active published template for <paramref name="kind"/> in the patient's
    /// <paramref name="preferredLanguageCode"/> (BCP-47), falling back to the primary subtag and
    /// then the language-neutral default per <see cref="Domain.ReportTemplateResolver"/>.
    /// </summary>
    Task<ReportTemplate?> FindActiveAsync(
        ReportKind kind,
        string? preferredLanguageCode,
        CancellationToken cancellationToken);
}

/// <summary>
/// Cross-module read port — builds the flat report context from session + MAR + alarms.
/// Implemented in the composition root because it needs to pull from multiple aggregates.
/// </summary>
public interface ISessionReportContextBuilder
{
    Task<SessionReportContext?> BuildAsync(Guid sessionId, CancellationToken cancellationToken);
}
